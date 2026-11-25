using DC_BOUTIQUE.Core.Domain.Helpers;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using DC_BOUTIQUE.Infrastructure.Persistence.Gateways;
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
            // Base de datos SQLite local autónoma en C:\ProgramData\DC_BOUTIQUE\Data\DC_BOUTIQUE.db
            var sqliteConn = DatabasePathHelper.GetSqliteConnectionString();

            services.AddDbContext<DCBoutiqueContext>(options =>
            {
                options.UseSqlite(sqliteConn);
                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });

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

            // Unit of Work y Gateway Técnico
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITechPersistenceGateway, TechPersistenceGateway>();

            return services;
        }
    }
}
