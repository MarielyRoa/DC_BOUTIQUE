using System.Data;
using System.Diagnostics;
using System.Text;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Helpers;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.Infrastructure.Persistence.Gateways
{
    /// <summary>
    /// Puerta de enlace técnica sobre SQLite y EF Core para el Centro del Desarrollador.
    /// Respeta la arquitectura Onion implementando ITechPersistenceGateway definida en Domain.
    /// </summary>
    public class TechPersistenceGateway : ITechPersistenceGateway
    {
        private static readonly string[] ExpectedCoreTables =
        {
            "Usuarios",
            "Categorias",
            "Productos",
            "ProductoVariantes",
            "Clientes",
            "TurnosCaja",
            "GastosCaja",
            "Ventas",
            "DetallesVenta",
            "PagosVenta",
            "MovimientosInventario",
            "RegistrosLogTecnico",
            "AuditoriasTecnicas",
            "ConfiguracionesRespaldos",
            "ConfiguracionesTecnicas",
            "RegistrosMantenimiento"
        };

        private static readonly byte[] SqliteHeaderMagic = Encoding.ASCII.GetBytes("SQLite format 3\0");

        private readonly DCBoutiqueContext _context;

        public TechPersistenceGateway(DCBoutiqueContext context)
        {
            _context = context;
        }

        public async Task<SqliteHealthSnapshot> GetSqliteHealthAsync(CancellationToken cancellationToken = default)
        {
            var dbPath = DatabasePathHelper.GetDatabaseFilePath();
            var snapshot = new SqliteHealthSnapshot
            {
                DatabasePath = dbPath,
                IsSqlite = _context.Database.IsSqlite()
            };

            try
            {
                if (File.Exists(dbPath))
                {
                    snapshot.FileSizeBytes = new FileInfo(dbPath).Length;
                }

                var walPath = dbPath + "-wal";
                if (File.Exists(walPath))
                {
                    snapshot.WalSizeBytes = new FileInfo(walPath).Length;
                }

                snapshot.CanConnect = await _context.Database.CanConnectAsync(cancellationToken);
                if (!snapshot.CanConnect)
                {
                    snapshot.ErrorMessage = "No se pudo establecer conexión con la base de datos SQLite.";
                    return snapshot;
                }

                var conn = _context.Database.GetDbConnection();
                var shouldClose = conn.State != ConnectionState.Open;
                if (shouldClose) await conn.OpenAsync(cancellationToken);

                try
                {
                    snapshot.SqliteVersion = await ExecuteScalarStringAsync(conn, "SELECT sqlite_version();", cancellationToken) ?? "N/D";
                    snapshot.JournalMode = (await ExecuteScalarStringAsync(conn, "PRAGMA journal_mode;", cancellationToken) ?? "N/D").ToUpperInvariant();
                    snapshot.ForeignKeysEnabled = (await ExecuteScalarLongAsync(conn, "PRAGMA foreign_keys;", cancellationToken)) == 1;
                    snapshot.PageCount = await ExecuteScalarLongAsync(conn, "PRAGMA page_count;", cancellationToken);
                    snapshot.PageSize = await ExecuteScalarLongAsync(conn, "PRAGMA page_size;", cancellationToken);
                    snapshot.FreelistCount = await ExecuteScalarLongAsync(conn, "PRAGMA freelist_count;", cancellationToken);

                    await using var tblCmd = conn.CreateCommand();
                    tblCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
                    await using var reader = await tblCmd.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        snapshot.TablesFound.Add(reader.GetString(0));
                    }

                    foreach (var expected in ExpectedCoreTables)
                    {
                        if (!snapshot.TablesFound.Any(t => t.Equals(expected, StringComparison.OrdinalIgnoreCase)))
                        {
                            snapshot.MissingExpectedTables.Add(expected);
                        }
                    }

                    bool hasEsPrincipal = false;
                    await using (var colCmd = conn.CreateCommand())
                    {
                        colCmd.CommandText = "PRAGMA table_info(\"Usuarios\");";
                        await using var colReader = await colCmd.ExecuteReaderAsync(cancellationToken);
                        while (await colReader.ReadAsync(cancellationToken))
                        {
                            if (string.Equals(colReader["name"]?.ToString(), "EsPrincipal", StringComparison.OrdinalIgnoreCase))
                            {
                                hasEsPrincipal = true;
                                break;
                            }
                        }
                    }

                    if (!hasEsPrincipal)
                    {
                        snapshot.MissingExpectedTables.Add("Usuarios.EsPrincipal (columna)");
                    }

                    snapshot.SchemaUpToDate = snapshot.MissingExpectedTables.Count == 0;
                }
                finally
                {
                    if (shouldClose) await conn.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                snapshot.ErrorMessage = ex.Message;
            }

            return snapshot;
        }

        public async Task<SystemCountsSnapshot> GetSystemCountsAsync(CancellationToken cancellationToken = default)
        {
            return new SystemCountsSnapshot
            {
                TotalUsuarios = await _context.Usuarios.AsNoTracking().CountAsync(cancellationToken),
                UsuariosActivos = await _context.Usuarios.AsNoTracking().CountAsync(u => u.Activo, cancellationToken),
                TotalProductos = await _context.Productos.AsNoTracking().CountAsync(p => p.Activo, cancellationToken),
                TotalVariantes = await _context.ProductoVariantes.AsNoTracking().CountAsync(v => v.Activo, cancellationToken)
            };
        }

        public async Task<SqliteIntegrityReport> RunIntegrityCheckAsync(CancellationToken cancellationToken = default)
        {
            return await VerifySqliteFileIntegrityAsync(DatabasePathHelper.GetDatabaseFilePath(), cancellationToken);
        }

        public async Task<SqliteIntegrityReport> VerifySqliteFileIntegrityAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var report = new SqliteIntegrityReport { CheckedAtUtc = DateTime.UtcNow };

            try
            {
                if (!File.Exists(filePath))
                {
                    report.IsValid = false;
                    report.Results.Add("El archivo de base de datos no existe en la ruta indicada.");
                    return report;
                }

                var header = new byte[16];
                await using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (fs.Length < 100)
                    {
                        report.IsValid = false;
                        report.Results.Add("El archivo es demasiado pequeño para ser una base de datos SQLite válida.");
                        return report;
                    }
                    await fs.ReadExactlyAsync(header, cancellationToken);
                }

                if (!header.SequenceEqual(SqliteHeaderMagic))
                {
                    report.IsValid = false;
                    report.Results.Add("La cabecera del archivo no corresponde a una base de datos SQLite 3 válida.");
                    return report;
                }

                var connString = new SqliteConnectionStringBuilder
                {
                    DataSource = filePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString();

                await using var conn = new SqliteConnection(connString);
                await conn.OpenAsync(cancellationToken);

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check;";
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    report.Results.Add(reader.GetString(0));
                }

                report.IsValid = report.Results.Count == 1
                    && report.Results[0].Equals("ok", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                report.IsValid = false;
                report.Results.Add($"Error al verificar integridad: {ex.Message}");
            }
            finally
            {
                sw.Stop();
                report.DurationMs = sw.ElapsedMilliseconds;
            }

            return report;
        }

        public async Task<(bool Success, string Message, long BytesWritten)> CreateOnlineSqliteBackupAsync(
            string destinationFilePath,
            CancellationToken cancellationToken = default)
        {
            var sourcePath = DatabasePathHelper.GetDatabaseFilePath();
            if (!File.Exists(sourcePath))
            {
                return (false, $"La base de datos SQLite no existe en: {sourcePath}", 0);
            }

            try
            {
                var destDir = Path.GetDirectoryName(destinationFilePath);
                if (!string.IsNullOrWhiteSpace(destDir))
                {
                    DatabasePathHelper.EnsureDirectoryExists(destDir);
                }

                if (File.Exists(destinationFilePath))
                {
                    File.Delete(destinationFilePath);
                }

                var srcConnStr = new SqliteConnectionStringBuilder
                {
                    DataSource = sourcePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString();

                var dstConnStr = new SqliteConnectionStringBuilder
                {
                    DataSource = destinationFilePath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Pooling = false
                }.ToString();

                await Task.Run(() =>
                {
                    using var source = new SqliteConnection(srcConnStr);
                    using var destination = new SqliteConnection(dstConnStr);
                    source.Open();
                    destination.Open();
                    // Respaldo consistente nativo de SQLite compatible con WAL
                    source.BackupDatabase(destination);
                }, cancellationToken);

                var verify = await VerifySqliteFileIntegrityAsync(destinationFilePath, cancellationToken);
                if (!verify.IsValid)
                {
                    if (File.Exists(destinationFilePath)) File.Delete(destinationFilePath);
                    return (false, $"El respaldo generado no superó la verificación de integridad: {string.Join("; ", verify.Results)}", 0);
                }

                var size = new FileInfo(destinationFilePath).Length;
                return (true, "Respaldo consistente SQLite generado y verificado con PRAGMA integrity_check.", size);
            }
            catch (Exception ex)
            {
                return (false, $"Error al crear respaldo consistente: {ex.Message}", 0);
            }
        }

        public async Task<SqliteRestoreOutcome> RestoreSqliteBackupSafelyAsync(
            string sourceBackupPath,
            string preRestoreBackupPath,
            CancellationToken cancellationToken = default)
        {
            var outcome = new SqliteRestoreOutcome();
            var liveDbPath = DatabasePathHelper.GetDatabaseFilePath();

            try
            {
                // Paso 1: Verificar integridad del respaldo seleccionado ANTES de modificar nada
                var backupCheck = await VerifySqliteFileIntegrityAsync(sourceBackupPath, cancellationToken);
                if (!backupCheck.IsValid)
                {
                    outcome.Success = false;
                    outcome.Message = $"Restauración cancelada: el archivo seleccionado no superó la verificación ({string.Join("; ", backupCheck.Results)}).";
                    return outcome;
                }

                // Paso 2: Respaldo automático preventivo del estado actual
                if (File.Exists(liveDbPath))
                {
                    var preBackup = await CreateOnlineSqliteBackupAsync(preRestoreBackupPath, cancellationToken);
                    if (!preBackup.Success)
                    {
                        outcome.Success = false;
                        outcome.Message = $"Restauración cancelada porque no pudo crearse el respaldo preventivo previo: {preBackup.Message}";
                        return outcome;
                    }

                    outcome.PreRestoreBackupPath = preRestoreBackupPath;
                    outcome.PreRestoreBackupFileName = Path.GetFileName(preRestoreBackupPath);
                }

                // Paso 3: Cerrar conexiones activas a SQLite y limpiar pools
                await _context.Database.CloseConnectionAsync();
                SqliteConnection.ClearAllPools();

                // Paso 4: Restaurar de forma atómica limpiando WAL/SHM residuales
                var tempTargetPath = liveDbPath + ".restoring";
                await Task.Run(() =>
                {
                    File.Copy(sourceBackupPath, tempTargetPath, overwrite: true);

                    var walPath = liveDbPath + "-wal";
                    var shmPath = liveDbPath + "-shm";
                    if (File.Exists(walPath)) File.Delete(walPath);
                    if (File.Exists(shmPath)) File.Delete(shmPath);

                    if (File.Exists(liveDbPath)) File.Delete(liveDbPath);
                    File.Move(tempTargetPath, liveDbPath);
                }, cancellationToken);

                // Paso 5: Verificar integridad posterior y revertir automáticamente si falla
                var postCheck = await VerifySqliteFileIntegrityAsync(liveDbPath, cancellationToken);
                if (!postCheck.IsValid)
                {
                    if (!string.IsNullOrWhiteSpace(outcome.PreRestoreBackupPath) && File.Exists(outcome.PreRestoreBackupPath))
                    {
                        SqliteConnection.ClearAllPools();
                        File.Copy(outcome.PreRestoreBackupPath, liveDbPath, overwrite: true);
                    }

                    outcome.Success = false;
                    outcome.Message = "La base restaurada falló la verificación posterior y se revirtió automáticamente al respaldo previo.";
                    return outcome;
                }

                // Paso 6: Reinicializar conexión, asegurar evolución de esquema y comprobar consulta real
                await DbInitializer.EnsureSqliteSchemaEvolutionAsync(_context);
                await _context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;", cancellationToken);

                var usersCount = await _context.Usuarios.AsNoTracking().CountAsync(cancellationToken);
                outcome.PostRestoreQueryVerified = usersCount >= 0;

                outcome.Success = true;
                outcome.Message = $"Base de datos restaurada y verificada correctamente ({usersCount} usuario(s) consultables). Respaldo previo guardado como '{outcome.PreRestoreBackupFileName}'.";
                return outcome;
            }
            catch (Exception ex)
            {
                outcome.Success = false;
                outcome.Message = $"Error durante la restauración segura: {ex.Message}";
                return outcome;
            }
        }

        public async Task<(bool Success, string Message)> RunControlledSchemaEvolutionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await DbInitializer.EnsureSqliteSchemaEvolutionAsync(_context);
                await _context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;", cancellationToken);
                return (true, "Verificación y evolución controlada del esquema SQLite completada correctamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Error al ejecutar verificación de esquema: {ex.Message}");
            }
        }

        public async Task AddLogAsync(RegistroLogTecnico log, CancellationToken cancellationToken = default)
        {
            await _context.RegistrosLogTecnico.AddAsync(log, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<RegistroLogTecnico>> QueryLogsAsync(
            DateTime? desdeUtc,
            DateTime? hastaUtc,
            TipoLogTecnico? tipo,
            string? severidad,
            string? searchText = null,
            int maxRows = 300,
            CancellationToken cancellationToken = default)
        {
            var query = _context.RegistrosLogTecnico.AsNoTracking().AsQueryable();

            if (desdeUtc.HasValue) query = query.Where(x => x.FechaEvento >= desdeUtc.Value);
            if (hastaUtc.HasValue) query = query.Where(x => x.FechaEvento <= hastaUtc.Value);
            if (tipo.HasValue) query = query.Where(x => x.Tipo == tipo.Value);

            if (!string.IsNullOrWhiteSpace(severidad))
            {
                var sev = severidad.Trim();
                query = query.Where(x => x.Severidad == sev);
            }

            var rows = await query
                .OrderByDescending(x => x.FechaEvento)
                .Take(Math.Clamp(maxRows, 1, 2000))
                .ToListAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var term = searchText.Trim();
                rows = rows.Where(x =>
                    x.Mensaje.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Origen.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.DetalleTecnico != null && x.DetalleTecnico.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (x.UsuarioRelacionado != null && x.UsuarioRelacionado.Contains(term, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return rows;
        }

        public async Task<int> CountLogsAsync(DateTime? desdeUtc = null, TipoLogTecnico? tipo = null, CancellationToken cancellationToken = default)
        {
            var query = _context.RegistrosLogTecnico.AsNoTracking().AsQueryable();
            if (desdeUtc.HasValue) query = query.Where(x => x.FechaEvento >= desdeUtc.Value);
            if (tipo.HasValue) query = query.Where(x => x.Tipo == tipo.Value);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<int> PurgeOldLogsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            var oldLogs = await _context.RegistrosLogTecnico
                .Where(x => x.FechaEvento < cutoffUtc)
                .ToListAsync(cancellationToken);

            if (oldLogs.Count == 0) return 0;

            _context.RegistrosLogTecnico.RemoveRange(oldLogs);
            await _context.SaveChangesAsync(cancellationToken);
            return oldLogs.Count;
        }

        public async Task AddAuditAsync(AuditoriaTecnica audit, CancellationToken cancellationToken = default)
        {
            await _context.AuditoriasTecnicas.AddAsync(audit, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<AuditoriaTecnica>> GetRecentAuditsAsync(int maxRows = 100, CancellationToken cancellationToken = default)
        {
            return await _context.AuditoriasTecnicas
                .AsNoTracking()
                .OrderByDescending(x => x.FechaAccion)
                .Take(Math.Clamp(maxRows, 1, 500))
                .ToListAsync(cancellationToken);
        }

        public async Task<ConfiguracionRespaldos> GetBackupConfigAsync(CancellationToken cancellationToken = default)
        {
            var cfg = await _context.ConfiguracionesRespaldos
                .OrderBy(x => x.FechaCreacion)
                .FirstOrDefaultAsync(cancellationToken);

            if (cfg == null)
            {
                cfg = new ConfiguracionRespaldos
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
                };
                await _context.ConfiguracionesRespaldos.AddAsync(cfg, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return cfg;
        }

        public async Task SaveBackupConfigAsync(ConfiguracionRespaldos config, CancellationToken cancellationToken = default)
        {
            var existing = await _context.ConfiguracionesRespaldos.FirstOrDefaultAsync(x => x.Id == config.Id, cancellationToken);
            if (existing == null)
            {
                await _context.ConfiguracionesRespaldos.AddAsync(config, cancellationToken);
            }
            else
            {
                existing.AutomaticosActivos = config.AutomaticosActivos;
                existing.Frecuencia = config.Frecuencia;
                existing.RespaldoDiarioActivo = config.RespaldoDiarioActivo;
                existing.HoraRespaldoDiario = config.HoraRespaldoDiario;
                existing.CarpetaDestino = config.CarpetaDestino;
                existing.RetenerHorarios = config.RetenerHorarios;
                existing.RetenerDiarios = config.RetenerDiarios;
                existing.RetenerSemanales = config.RetenerSemanales;
                existing.RetenerMensuales = config.RetenerMensuales;
                existing.RetencionDias = config.RetencionDias;
                existing.ConservarDiarios = config.ConservarDiarios;
                existing.ConservarSemanales = config.ConservarSemanales;
                existing.ConservarMensuales = config.ConservarMensuales;
                existing.UltimoRespaldoExitosoUtc = config.UltimoRespaldoExitosoUtc;
                existing.UltimoArchivoRespaldo = config.UltimoArchivoRespaldo;
                existing.UltimoRespaldoDiarioUtc = config.UltimoRespaldoDiarioUtc;
                existing.ProximoRespaldoProgramadoUtc = config.ProximoRespaldoProgramadoUtc;
                existing.UltimoErrorRespaldo = config.UltimoErrorRespaldo;
                existing.FechaUltimoErrorUtc = config.FechaUltimoErrorUtc;
                _context.ConfiguracionesRespaldos.Update(existing);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<ConfiguracionTecnica> GetTechConfigAsync(CancellationToken cancellationToken = default)
        {
            var cfg = await _context.ConfiguracionesTecnicas
                .OrderBy(x => x.FechaCreacion)
                .FirstOrDefaultAsync(cancellationToken);

            if (cfg == null)
            {
                cfg = new ConfiguracionTecnica
                {
                    Id = Guid.NewGuid(),
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };
                await _context.ConfiguracionesTecnicas.AddAsync(cfg, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return cfg;
        }

        public async Task SaveTechConfigAsync(ConfiguracionTecnica config, CancellationToken cancellationToken = default)
        {
            var existing = await _context.ConfiguracionesTecnicas.FirstOrDefaultAsync(x => x.Id == config.Id, cancellationToken);
            if (existing == null)
            {
                await _context.ConfiguracionesTecnicas.AddAsync(config, cancellationToken);
            }
            else
            {
                existing.ImpresoraConfigurada = config.ImpresoraConfigurada;
                existing.AnchoPapelMm = config.AnchoPapelMm;
                existing.NombreComercialTicket = config.NombreComercialTicket;
                existing.DireccionTicket = config.DireccionTicket;
                existing.TelefonoTicket = config.TelefonoTicket;
                existing.MensajePieTicket = config.MensajePieTicket;
                existing.Moneda = config.Moneda;
                existing.FormatoFecha = config.FormatoFecha;
                existing.ZonaHoraria = config.ZonaHoraria;
                existing.CulturaRegional = config.CulturaRegional;
                existing.VersionRegistrada = config.VersionRegistrada;
                _context.ConfiguracionesTecnicas.Update(existing);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task AddMaintenanceRecordAsync(RegistroMantenimiento record, CancellationToken cancellationToken = default)
        {
            await _context.RegistrosMantenimiento.AddAsync(record, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<RegistroMantenimiento>> QueryMaintenanceRecordsAsync(
            DateTime? desdeUtc,
            DateTime? hastaUtc,
            TipoMantenimiento? tipo,
            CancellationToken cancellationToken = default)
        {
            var query = _context.RegistrosMantenimiento.AsNoTracking().AsQueryable();
            if (desdeUtc.HasValue) query = query.Where(x => x.FechaMantenimientoUtc >= desdeUtc.Value);
            if (hastaUtc.HasValue) query = query.Where(x => x.FechaMantenimientoUtc <= hastaUtc.Value);
            if (tipo.HasValue) query = query.Where(x => x.Tipo == tipo.Value);

            return await query
                .OrderByDescending(x => x.FechaMantenimientoUtc)
                .Take(300)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> UserHasCommercialOperationsAsync(Guid usuarioId, CancellationToken cancellationToken = default)
        {
            bool hasVentas = await _context.Ventas.AsNoTracking().AnyAsync(v => v.UsuarioId == usuarioId, cancellationToken);
            if (hasVentas) return true;

            bool hasTurnos = await _context.TurnosCaja.AsNoTracking().AnyAsync(t => t.UsuarioId == usuarioId, cancellationToken);
            if (hasTurnos) return true;

            return await _context.MovimientosInventario.AsNoTracking().AnyAsync(m => m.UsuarioId == usuarioId, cancellationToken);
        }

        public async Task UpdateUserLastAccessAsync(Guid usuarioId, DateTime utcNow, CancellationToken cancellationToken = default)
        {
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, cancellationToken);
            if (user != null)
            {
                user.UltimoAcceso = utcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private static async Task<string?> ExecuteScalarStringAsync(System.Data.Common.DbConnection conn, string sql, CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = await cmd.ExecuteScalarAsync(ct);
            return result?.ToString();
        }

        private static async Task<long> ExecuteScalarLongAsync(System.Data.Common.DbConnection conn, string sql, CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = await cmd.ExecuteScalarAsync(ct);
            return result != null && result != DBNull.Value ? Convert.ToInt64(result) : 0L;
        }
    }
}
