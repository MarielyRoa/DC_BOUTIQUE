using DC_BOUTIQUE.Core.Domain.Helpers;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using DC_BOUTIQUE.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DC_BOUTIQUE.Infrastructure.Persistence
{
    public static class ServicesRegistration
    {
        public static IServiceCollection AddPersistenceInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Proveedor por defecto: "Sqlite" para distribución cliente autónoma sin servidores
            var provider = configuration["DatabaseProvider"] ?? "Sqlite";

            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                // Resuelve dinámicamente C:\ProgramData\DC_BOUTIQUE\Data\DC_BOUTIQUE.db mediante Environment.SpecialFolder
                var sqliteConn = DatabasePathHelper.GetSqliteConnectionString();

                services.AddDbContext<DCBoutiqueContext>(options =>
                {
                    options.UseSqlite(sqliteConn);
                    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
                });
            }
            else
            {
                var sqlServerConn = configuration.GetConnectionString("DefaultConnection") 
                    ?? "Server=Gerardine\\SQLEXPRESS;Database=DC_BOUTIQUE;Trusted_Connection=True;TrustServerCertificate=true";

                services.AddDbContext<DCBoutiqueContext>(options =>
                {
                    options.UseSqlServer(sqlServerConn, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(DCBoutiqueContext).Assembly.FullName);
                    });
                    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
                });
            }

            // Registro de Repositorios e interfaces de Domain
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<ICategoriaRepository, CategoriaRepository>();
            services.AddScoped<IProductoRepository, ProductoRepository>();
            services.AddScoped<IProductoVarianteRepository, ProductoVarianteRepository>();
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<ITurnoCajaRepository, TurnoCajaRepository>();
            services.AddScoped<IGastoCajaRepository, GastoCajaRepository>();
            services.AddScoped<IVentaRepository, VentaRepository>();
            services.AddScoped<IMovimientoInventarioRepository, MovimientoInventarioRepository>();

            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
