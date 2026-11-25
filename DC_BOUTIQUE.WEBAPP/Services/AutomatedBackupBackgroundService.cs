using DC_BOUTIQUE.Core.Application.Interfaces;

namespace DC_BOUTIQUE.WEBAPP.Services
{
    /// <summary>
    /// Servicio en segundo plano para respaldos automáticos de SQLite.
    /// Se ejecuta mientras la aplicación esté abierta, detecta respaldos pendientes al arrancar
    /// (si la PC se apagó o la aplicación estuvo cerrada) y evita ejecuciones simultáneas mediante SemaphoreSlim.
    /// </summary>
    public class AutomatedBackupBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutomatedBackupBackgroundService> _logger;

        public AutomatedBackupBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<AutomatedBackupBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Espera inicial breve para asegurar que la BD SQLite terminó su inicialización
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var techService = scope.ServiceProvider.GetRequiredService<ITechAdminService>();
                    await techService.RunScheduledBackupIfDueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el ciclo de respaldos automáticos programados.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
