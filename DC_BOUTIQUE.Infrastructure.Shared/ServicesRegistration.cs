using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Infrastructure.Shared.Backup;
using DC_BOUTIQUE.Infrastructure.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DC_BOUTIQUE.Infrastructure.Shared
{
    public static class ServicesRegistration
    {
        public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            #region Backup Manager & Service
            services.AddSingleton<DatabaseBackupManager>();
            services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
            #endregion

            #region Printing Service
            services.AddScoped<ITicketPrintingService, TicketPrintingService>();
            #endregion

            #region Common & DateTime Services
            services.AddTransient<IDateTimeService, DateTimeService>();
            #endregion

            return services;
        }
    }
}
