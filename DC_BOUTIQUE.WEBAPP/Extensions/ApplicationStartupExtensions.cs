using System.Data;
using System.Diagnostics;
using DC_BOUTIQUE.Core.Application.DTOs.Usuarios;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Helpers;
using DC_BOUTIQUE.Infrastructure.Identity.Contexts;
using DC_BOUTIQUE.Infrastructure.Identity.Entities;
using DC_BOUTIQUE.Infrastructure.Identity.Seeds;
using DC_BOUTIQUE.Infrastructure.Persistence;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using DC_BOUTIQUE.WEBAPP.Middlewares;
using DC_BOUTIQUE.WEBAPP.Services;
using Microsoft.AspNetCore.DataProtection;
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
        /// Registra MVC, Antiforgery, llaves persistentes de DataProtection y la sesión local de turno (8 horas).
        /// </summary>
        public static IServiceCollection AddWebAppServices(this IServiceCollection services)
        {
            services.AddControllersWithViews();

            // Persistir llaves de cifrado de cookies en ProgramData para evitar advertencias de sesión entre reinicios
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(DatabasePathHelper.GetDataProtectionKeysDirectory()))
                .SetApplicationName("DC_BOUTIQUE");

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
            services.AddHostedService<AutomatedBackupBackgroundService>();

            return services;
        }

        /// <summary>
        /// Crea e inicializa de forma segura la base de datos SQLite local en C:\ProgramData\DC_BOUTIQUE\Data,
        /// verificando primero sqlite_master para no emitir errores de tablas duplicadas y conservando siempre los datos.
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

                    if (!await TableExistsInSqliteAsync(identityContext, "Roles"))
                    {
                        await creator.CreateTablesAsync();
                    }
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
                await DefaultDeveloperUser.SeedAsync(userManager);

                // Garantizar perfil de dominio para la cuenta Desarrollador en cualquier instalación nueva o existente
                var usuarioService = services.GetRequiredService<IUsuarioService>();
                if (!await usuarioService.ExisteDesarrolladorAsync())
                {
                    await usuarioService.CrearDesarrolladorInicialAsync(new SaveUsuarioDto
                    {
                        NombreCompleto = $"{DefaultDeveloperUser.DefaultFirstName} {DefaultDeveloperUser.DefaultLastName}".Trim(),
                        NombreUsuario = DefaultDeveloperUser.DefaultUserName,
                        Password = DefaultDeveloperUser.DefaultPassword,
                        Rol = RolUsuario.Desarrollador,
                        Activo = true
                    });
                }
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
        /// Abre DC_BOUTIQUE como una ventana de aplicación de escritorio independiente (modo --app sin pestañas
        /// ni barra de direcciones) usando Microsoft Edge o Chrome, y detiene el servidor al cerrar la ventana.
        /// </summary>
        public static void LaunchBrowserOnLocalStart(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment() && Environment.GetEnvironmentVariable("DC_NO_BROWSER") != "1")
            {
                app.Lifetime.ApplicationStarted.Register(() =>
                {
                    var url = app.Urls.FirstOrDefault() ?? "http://localhost:5055";
                    OpenDesktopWindow(url, () => app.Lifetime.StopApplication());
                });
            }
        }

        public static void OpenDesktopWindow(string url, Action? onWindowClosed = null)
        {
            try
            {
                var profileDir = DatabasePathHelper.GetAppWindowProfileDirectory();
                var browserExe = FindDesktopWebViewBrowser();

                if (!string.IsNullOrEmpty(browserExe))
                {
                    var proc = Process.Start(new ProcessStartInfo
                    {
                        FileName = browserExe,
                        Arguments = $"--app=\"{url}\" --user-data-dir=\"{profileDir}\" --start-maximized --no-first-run --no-default-browser-check",
                        UseShellExecute = false
                    });

                    if (proc != null && onWindowClosed != null)
                    {
                        proc.EnableRaisingEvents = true;
                        proc.Exited += (_, _) => onWindowClosed();
                    }
                }
                else
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            catch
            {
                // No interrumpir el servidor si falla el lanzamiento automático
            }
        }

        private static string? FindDesktopWebViewBrowser()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe")
            };

            return candidates.FirstOrDefault(File.Exists);
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
    }
}
