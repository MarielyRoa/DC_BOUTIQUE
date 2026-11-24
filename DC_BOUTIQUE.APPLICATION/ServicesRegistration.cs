using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DC_BOUTIQUE.Core.Application
{
    public static class ServicesRegistration
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            #region Services
            services.AddScoped<ICategoriaService, CategoriaService>();
            services.AddScoped<IProductoService, ProductoService>();
            services.AddScoped<IClienteService, ClienteService>();
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<ICajaService, CajaService>();
            services.AddScoped<IVentaService, VentaService>();
            services.AddScoped<IInventarioService, InventarioService>();
            #endregion

            return services;
        }
    }
}
