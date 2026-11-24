using System.Security.Cryptography;
using System.Text;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(DCBoutiqueContext context)
        {
            if (context.Database.IsSqlite())
            {
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                await context.Database.MigrateAsync();
            }

            // 1. Usuarios base del sistema (Admin y Cajera)
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
                    PorcentajeComision = 2.0m,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                await context.Usuarios.AddRangeAsync(admin, cajera);
            }

            // 2. Cliente de Mostrador (Consumidor Final para ventas sin RNC)
            if (!await context.Clientes.AnyAsync())
            {
                var clienteFinal = new Cliente
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
                };

                await context.Clientes.AddAsync(clienteFinal);
            }

            // 3. Limpiar de la base de datos las categorías que hayan quedado en estado inactivo
            var categoriasInactivas = await context.Categorias.Where(c => !c.Activo).ToListAsync();
            if (categoriasInactivas.Any())
            {
                context.Categorias.RemoveRange(categoriasInactivas);
            }

            // 4. Limpiar turnos de caja antiguos cerrados (solo conservar el turno abierto en curso)
            var demoCutoff = new DateTime(2026, 11, 24, 7, 0, 0, DateTimeKind.Utc);
            var turnosCerradosAntiguos = await context.TurnosCaja
                .Where(t => t.Estado != EstadoCaja.Abierta && t.FechaApertura < demoCutoff)
                .ToListAsync();

            if (turnosCerradosAntiguos.Any())
            {
                var turnosIds = turnosCerradosAntiguos.Select(t => t.Id).ToList();

                var ventasAntiguas = await context.Ventas
                    .Where(v => turnosIds.Contains(v.TurnoCajaId))
                    .ToListAsync();

                if (ventasAntiguas.Any())
                {
                    context.Ventas.RemoveRange(ventasAntiguas);
                }

                var gastosAntiguos = await context.GastosCaja
                    .Where(g => g.TurnoCajaId.HasValue && turnosIds.Contains(g.TurnoCajaId.Value))
                    .ToListAsync();

                if (gastosAntiguos.Any())
                {
                    context.GastosCaja.RemoveRange(gastosAntiguos);
                }

                context.TurnosCaja.RemoveRange(turnosCerradosAntiguos);
            }

            // 5. Ajustar ventas registradas con hora UTC (+4h) a hora local dominicana
            var ventasUtc = await context.Ventas
                .Where(v => v.FechaVenta >= new DateTime(2026, 11, 24, 7, 0, 0) && v.FechaVenta <= new DateTime(2026, 11, 24, 12, 0, 0))
                .ToListAsync();
            if (ventasUtc.Any())
            {
                foreach (var v in ventasUtc)
                {
                    v.FechaVenta = v.FechaVenta.AddHours(-4);
                }
            }

            await context.SaveChangesAsync();
        }

        private static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
