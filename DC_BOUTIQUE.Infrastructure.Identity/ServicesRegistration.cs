using DC_BOUTIQUE.Core.Domain.Helpers;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Infrastructure.Identity.Contexts;
using DC_BOUTIQUE.Infrastructure.Identity.Entities;
using DC_BOUTIQUE.Infrastructure.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DC_BOUTIQUE.Infrastructure.Identity
{
    public static class ServicesRegistration
    {
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Base de datos SQLite local autónoma en C:\ProgramData\DC_BOUTIQUE\Data\DC_BOUTIQUE.db
            var sqliteConn = DatabasePathHelper.GetSqliteConnectionString();

            services.AddDbContext<IdentityContext>(options =>
            {
                options.UseSqlite(sqliteConn);
                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });

            #region Identity Configuration
            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                // Sistema puramente local de boutique (sin requerimientos de correo ni confirmaciones web)
                options.User.RequireUniqueEmail = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                // Contraseñas y PINs sencillos para operadoras de mostrador y cajeras
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;
            })
            .AddEntityFrameworkStores<IdentityContext>()
            .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/Auth/Login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });
            #endregion

            #region Services
            services.AddScoped<IAccountService, AccountServiceWebApp>();
            #endregion

            return services;
        }
    }
}
