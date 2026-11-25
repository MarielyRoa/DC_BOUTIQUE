using DC_BOUTIQUE.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace DC_BOUTIQUE.Infrastructure.Persistence.Contexts
{
    public class DCBoutiqueContext : DbContext
    {
        public DCBoutiqueContext(DbContextOptions<DCBoutiqueContext> options) : base(options) { }

        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<ProductoVariante> ProductoVariantes => Set<ProductoVariante>();
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<TurnoCaja> TurnosCaja => Set<TurnoCaja>();
        public DbSet<GastoCaja> GastosCaja => Set<GastoCaja>();
        public DbSet<Venta> Ventas => Set<Venta>();
        public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
        public DbSet<PagoVenta> PagosVenta => Set<PagoVenta>();
        public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();

        // Entidades del Centro del Desarrollador
        public DbSet<RegistroLogTecnico> RegistrosLogTecnico => Set<RegistroLogTecnico>();
        public DbSet<AuditoriaTecnica> AuditoriasTecnicas => Set<AuditoriaTecnica>();
        public DbSet<ConfiguracionRespaldos> ConfiguracionesRespaldos => Set<ConfiguracionRespaldos>();
        public DbSet<ConfiguracionTecnica> ConfiguracionesTecnicas => Set<ConfiguracionTecnica>();
        public DbSet<RegistroMantenimiento> RegistrosMantenimiento => Set<RegistroMantenimiento>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
