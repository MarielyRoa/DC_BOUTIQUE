using System.Diagnostics;
using DC_BOUTIQUE.Core.Domain.Helpers;
using DC_BOUTIQUE.Infrastructure.Identity.Contexts;
using DC_BOUTIQUE.Infrastructure.Identity.Entities;
using DC_BOUTIQUE.Infrastructure.Identity.Seeds;
using DC_BOUTIQUE.Infrastructure.Persistence;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using DC_BOUTIQUE.WEBAPP.Middlewares;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.FileProviders;

namespace DC_BOUTIQUE.WEBAPP.Extensions
{
    public static class ApplicationStartupExtensions
    {
        /// <summary>
        /// Configura un puerto HTTP local estable (http://localhost:5055) cuando se ejecuta
        /// como aplicación instalada en PCs clientes sin Visual Studio.
        /// </summary>
        public static void ConfigureLocalDesktopHost(this WebApplicationBuilder builder, string[] args)
        {
            if (!builder.Environment.IsDevelopment()
                && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
                && !args.Any(a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase)))
            {
                builder.WebHost.UseUrls("http://localhost:5055");
            }
        }

        /// <summary>
        /// Registra MVC, Antiforgery y la sesión local de turno de caja (8 horas).
        /// </summary>
        public static IServiceCollection AddWebAppServices(this IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddAntiforgery(options =>
            {
                options.HeaderName = "RequestVerificationToken";
            });

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(8);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.Name = ".DC_BOUTIQUE.Session";
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ValidateUserSession>();

            return services;
        }

        /// <summary>
        /// Crea e inicializa de forma segura la base de datos SQLite local en C:\ProgramData\DC_BOUTIQUE\Data,
        /// conservando siempre los datos existentes si ya fue creada previamente.
        /// </summary>
        public static async Task InitializeLocalDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<DCBoutiqueContext>();
                await DbInitializer.SeedAsync(context);

                if (context.Database.IsSqlite())
                {
                    await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;");
                }

                var identityContext = services.GetRequiredService<IdentityContext>();
                if (identityContext.Database.IsSqlite())
                {
                    var creator = identityContext.Database.GetService<IRelationalDatabaseCreator>();
                    if (!await creator.ExistsAsync())
                    {
                        await creator.CreateAsync();
                    }
                    try { await creator.CreateTablesAsync(); } catch { }
                }
                else
                {
                    await identityContext.Database.MigrateAsync();
                }

                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<AppUser>>();

                await DefaultUserRoles.SeedAsync(roleManager);
                await DefaultAdminUser.SeedAsync(userManager);
                await DefaultCashierUser.SeedAsync(userManager);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Ocurrió un error al inicializar la base de datos local.");
            }
        }

        /// <summary>
        /// Sirve los archivos estáticos de wwwroot y las fotos de prendas almacenadas en C:\ProgramData\DC_BOUTIQUE\Images.
        /// </summary>
        public static void UseLocalStaticStorage(this WebApplication app)
        {
            app.UseStaticFiles();

            DatabasePathHelper.GetBackupDirectory();
            var imagesProductosPath = DatabasePathHelper.GetImagesDirectory("productos");

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(imagesProductosPath),
                RequestPath = "/images/productos"
            });
        }

        /// <summary>
        /// Abre automáticamente el navegador predeterminado al ejecutar el acceso directo instalado en Windows.
        /// </summary>
        public static void LaunchBrowserOnLocalStart(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment() && Environment.GetEnvironmentVariable("DC_NO_BROWSER") != "1")
            {
                app.Lifetime.ApplicationStarted.Register(() =>
                {
                    try
                    {
                        var url = app.Urls.FirstOrDefault() ?? "http://localhost:5055";
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    catch { }
                });
            }
        }
    }
}
