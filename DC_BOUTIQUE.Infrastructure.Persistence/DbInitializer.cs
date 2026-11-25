using System.Data;
using System.Security.Cryptography;
using System.Text;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Helpers;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace DC_BOUTIQUE.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(DCBoutiqueContext context)
        {
            var creator = context.Database.GetService<IRelationalDatabaseCreator>();
            if (!await creator.ExistsAsync())
            {
                await creator.CreateAsync();
            }

            bool tablasExisten = await TableExistsInSqliteAsync(context, "Categorias");
            if (!tablasExisten)
            {
                await creator.CreateTablesAsync();
            }

            // Evolución segura del esquema SQLite para instalaciones ya existentes (sin perder datos)
            await EnsureSqliteSchemaEvolutionAsync(context);

            // 1. Usuarios base del sistema (Admin principal y Cajera)
            if (!await context.Usuarios.AnyAsync())
            {
                var admin = new Usuario
                {
                    Id = Guid.NewGuid(),
                    NombreCompleto = "Dahiana Corporan",
                    NombreUsuario = "admin",
                    PasswordHash = HashPassword("Admin123$"),
                    Cedula = "402-0000000-0",
                    Telefono = "809-555-0101",
                    Rol = RolUsuario.Administrador,
                    EsPrincipal = true,
                    PorcentajeComision = 0m,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var cajera = new Usuario
                {
                    Id = Guid.NewGuid(),
                    NombreCompleto = "Cajera Principal",
                    NombreUsuario = "caja01",
                    PasswordHash = HashPassword("Caja1234$"),
                    Cedula = "402-0000000-1",
                    Telefono = "809-555-0102",
                    Rol = RolUsuario.Vendedor,
                    EsPrincipal = false,
                    PorcentajeComision = 2.0m,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                await context.Usuarios.AddRangeAsync(admin, cajera);
            }
            else
            {
                // Garantizar siempre un Administrador Principal marcado con EsPrincipal = true
                bool existePrincipal = await context.Usuarios.AnyAsync(u => u.EsPrincipal && u.Rol == RolUsuario.Administrador);
                if (!existePrincipal)
                {
                    var adminOriginal = await context.Usuarios
                        .Where(u => u.Rol == RolUsuario.Administrador)
                        .OrderBy(u => u.NombreUsuario == "admin" ? 0 : 1)
                        .ThenBy(u => u.FechaCreacion)
                        .FirstOrDefaultAsync();

                    if (adminOriginal != null)
                    {
                        adminOriginal.EsPrincipal = true;
                        context.Usuarios.Update(adminOriginal);
                    }
                }
            }

            // 2. Cliente de Mostrador (Consumidor Final para ventas sin RNC)
            if (!await context.Clientes.AnyAsync())
            {
                await context.Clientes.AddAsync(new Cliente
                {
                    Id = Guid.NewGuid(),
                    Nombres = "Consumidor",
                    Apellidos = "Final",
                    CedulaORnc = null,
                    Telefono = "809-000-0000",
                    Correo = null,
                    Direccion = "Mostrador Boutique",
                    Notas = "Cliente general para ventas de mostrador al contado",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                });
            }

            // 3. Configuración inicial recomendada de respaldos automáticos
            if (!await context.ConfiguracionesRespaldos.AnyAsync())
            {
                await context.ConfiguracionesRespaldos.AddAsync(new ConfiguracionRespaldos
                {
                    Id = Guid.NewGuid(),
                    AutomaticosActivos = true,
                    Frecuencia = FrecuenciaRespaldo.Cada2Horas,
                    RespaldoDiarioActivo = true,
                    HoraRespaldoDiario = "20:00",
                    CarpetaDestino = DatabasePathHelper.GetBackupDirectory(),
                    RetenerHorarios = 12,
                    RetenerDiarios = 7,
                    RetenerSemanales = 4,
                    RetenerMensuales = 3,
                    RetencionDias = 90,
                    ConservarDiarios = true,
                    ConservarSemanales = true,
                    ConservarMensuales = true,
                    ProximoRespaldoProgramadoUtc = DateTime.UtcNow.AddHours(2),
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                });
            }

            // 4. Configuración técnica inicial (Impresión 80mm, Moneda DOP, Zona horaria RD)
            if (!await context.ConfiguracionesTecnicas.AnyAsync())
            {
                await context.ConfiguracionesTecnicas.AddAsync(new ConfiguracionTecnica
                {
                    Id = Guid.NewGuid(),
                    ImpresoraConfigurada = "Predeterminada de Windows",
                    AnchoPapelMm = 80,
                    NombreComercialTicket = "DAHIANA CORPORAN BOUTIQUE",
                    DireccionTicket = "Calle Principal, República Dominicana",
                    TelefonoTicket = "(809) 000-0000",
                    MensajePieTicket = "¡Gracias por su compra! Cambios dentro de los 7 días con este ticket.",
                    Moneda = "DOP",
                    FormatoFecha = "dd/MM/yyyy",
                    ZonaHoraria = "America/Santo_Domingo",
                    CulturaRegional = "es-DO",
                    VersionRegistrada = "1.0.0",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                });
            }

            // 5. Registro inicial en Historial de Mantenimiento
            if (!await context.RegistrosMantenimiento.AnyAsync())
            {
                await context.RegistrosMantenimiento.AddAsync(new RegistroMantenimiento
                {
                    Id = Guid.NewGuid(),
                    FechaMantenimientoUtc = DateTime.UtcNow,
                    Tipo = TipoMantenimiento.ActualizacionSistema,
                    DescripcionProblema = "Instalación / inicialización de DC_BOUTIQUE con Centro del Desarrollador.",
                    SolucionAplicada = "Base de datos SQLite local inicializada con respaldo automático configurable y diagnóstico integrado.",
                    VersionSistema = "1.0.0",
                    VersionAnterior = "0.0.0",
                    RespaldoPrevioRealizado = false,
                    Resultado = "Exitoso",
                    Observaciones = "Datos almacenados fuera de Program Files en C:\\ProgramData\\DC_BOUTIQUE.",
                    RegistradoPor = "Sistema",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                });
            }

            // 6. Limpiar categorías inactivas residuales
            var categoriasInactivas = await context.Categorias.Where(c => !c.Activo).ToListAsync();
            if (categoriasInactivas.Any())
            {
                context.Categorias.RemoveRange(categoriasInactivas);
            }

            await context.SaveChangesAsync();
        }

        public static async Task EnsureSqliteSchemaEvolutionAsync(DCBoutiqueContext context)
        {
            if (!context.Database.IsSqlite()) return;

            if (!await ColumnExistsInSqliteAsync(context, "Usuarios", "EsPrincipal"))
            {
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"Usuarios\" ADD COLUMN \"EsPrincipal\" INTEGER NOT NULL DEFAULT 0;");
            }

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""RegistrosLogTecnico"" (
                    ""Id"" TEXT NOT NULL CONSTRAINT ""PK_RegistrosLogTecnico"" PRIMARY KEY,
                    ""FechaEvento"" TEXT NOT NULL,
                    ""Tipo"" INTEGER NOT NULL,
                    ""Severidad"" TEXT NOT NULL,
                    ""Origen"" TEXT NOT NULL,
                    ""Mensaje"" TEXT NOT NULL,
                    ""DetalleTecnico"" TEXT NULL,
                    ""UsuarioRelacionado"" TEXT NULL,
                    ""FechaCreacion"" TEXT NOT NULL,
                    ""Activo"" INTEGER NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_RegistrosLogTecnico_FechaEvento"" ON ""RegistrosLogTecnico"" (""FechaEvento"");
                CREATE INDEX IF NOT EXISTS ""IX_RegistrosLogTecnico_Tipo"" ON ""RegistrosLogTecnico"" (""Tipo"");

                CREATE TABLE IF NOT EXISTS ""AuditoriasTecnicas"" (
                    ""Id"" TEXT NOT NULL CONSTRAINT ""PK_AuditoriasTecnicas"" PRIMARY KEY,
                    ""FechaAccion"" TEXT NOT NULL,
                    ""UsuarioTecnico"" TEXT NOT NULL,
                    ""Modulo"" TEXT NOT NULL,
                    ""Accion"" TEXT NOT NULL,
                    ""Exitosa"" INTEGER NOT NULL,
                    ""Detalle"" TEXT NULL,
                    ""FechaCreacion"" TEXT NOT NULL,
                    ""Activo"" INTEGER NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_AuditoriasTecnicas_FechaAccion"" ON ""AuditoriasTecnicas"" (""FechaAccion"");

                CREATE TABLE IF NOT EXISTS ""ConfiguracionesRespaldos"" (
                    ""Id"" TEXT NOT NULL CONSTRAINT ""PK_ConfiguracionesRespaldos"" PRIMARY KEY,
                    ""AutomaticosActivos"" INTEGER NOT NULL,
                    ""Frecuencia"" INTEGER NOT NULL,
                    ""RespaldoDiarioActivo"" INTEGER NOT NULL,
                    ""HoraRespaldoDiario"" TEXT NOT NULL,
                    ""CarpetaDestino"" TEXT NOT NULL,
                    ""RetenerHorarios"" INTEGER NOT NULL,
                    ""RetenerDiarios"" INTEGER NOT NULL,
                    ""RetenerSemanales"" INTEGER NOT NULL,
                    ""RetenerMensuales"" INTEGER NOT NULL,
                    ""RetencionDias"" INTEGER NOT NULL,
                    ""ConservarDiarios"" INTEGER NOT NULL,
                    ""ConservarSemanales"" INTEGER NOT NULL,
                    ""ConservarMensuales"" INTEGER NOT NULL,
                    ""UltimoRespaldoExitosoUtc"" TEXT NULL,
                    ""UltimoArchivoRespaldo"" TEXT NULL,
                    ""UltimoRespaldoDiarioUtc"" TEXT NULL,
                    ""ProximoRespaldoProgramadoUtc"" TEXT NULL,
                    ""UltimoErrorRespaldo"" TEXT NULL,
                    ""FechaUltimoErrorUtc"" TEXT NULL,
                    ""FechaCreacion"" TEXT NOT NULL,
                    ""Activo"" INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ""ConfiguracionesTecnicas"" (
                    ""Id"" TEXT NOT NULL CONSTRAINT ""PK_ConfiguracionesTecnicas"" PRIMARY KEY,
                    ""ImpresoraConfigurada"" TEXT NOT NULL,
                    ""AnchoPapelMm"" INTEGER NOT NULL,
                    ""NombreComercialTicket"" TEXT NOT NULL,
                    ""DireccionTicket"" TEXT NOT NULL,
                    ""TelefonoTicket"" TEXT NOT NULL,
                    ""MensajePieTicket"" TEXT NOT NULL,
                    ""Moneda"" TEXT NOT NULL,
                    ""FormatoFecha"" TEXT NOT NULL,
                    ""ZonaHoraria"" TEXT NOT NULL,
                    ""CulturaRegional"" TEXT NOT NULL,
                    ""VersionRegistrada"" TEXT NOT NULL,
                    ""FechaCreacion"" TEXT NOT NULL,
                    ""Activo"" INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ""RegistrosMantenimiento"" (
                    ""Id"" TEXT NOT NULL CONSTRAINT ""PK_RegistrosMantenimiento"" PRIMARY KEY,
                    ""FechaMantenimientoUtc"" TEXT NOT NULL,
                    ""Tipo"" INTEGER NOT NULL,
                    ""DescripcionProblema"" TEXT NOT NULL,
                    ""SolucionAplicada"" TEXT NOT NULL,
                    ""VersionSistema"" TEXT NOT NULL,
                    ""VersionAnterior"" TEXT NULL,
                    ""RespaldoPrevioRealizado"" INTEGER NOT NULL,
                    ""Resultado"" TEXT NOT NULL,
                    ""Observaciones"" TEXT NULL,
                    ""RegistradoPor"" TEXT NOT NULL,
                    ""FechaCreacion"" TEXT NOT NULL,
                    ""Activo"" INTEGER NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_RegistrosMantenimiento_FechaMantenimientoUtc"" ON ""RegistrosMantenimiento"" (""FechaMantenimientoUtc"");
                CREATE INDEX IF NOT EXISTS ""IX_RegistrosMantenimiento_Tipo"" ON ""RegistrosMantenimiento"" (""Tipo"");
            ");
        }

        private static async Task<bool> TableExistsInSqliteAsync(DbContext context, string tableName)
        {
            var conn = context.Database.GetDbConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) await conn.OpenAsync();
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name;";
                var param = cmd.CreateParameter();
                param.ParameterName = "@name";
                param.Value = tableName;
                cmd.Parameters.Add(param);
                var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                return count > 0;
            }
            finally
            {
                if (shouldClose) await conn.CloseAsync();
            }
        }

        private static async Task<bool> ColumnExistsInSqliteAsync(DbContext context, string tableName, string columnName)
        {
            var conn = context.Database.GetDbConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) await conn.OpenAsync();
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $"PRAGMA table_info(\"{tableName}\");";
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var name = reader["name"]?.ToString();
                    if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                if (shouldClose) await conn.CloseAsync();
            }
        }

        private static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
