using DC_BOUTIQUE.Core.Application.DTOs.TechAdmin;
using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public interface ITechAdminService
    {
        // 1. Dashboard Técnico
        Task<TechDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

        // 2. Base de Datos y Respaldos (Manuales, Automáticos y Restauración Segura)
        Task<TechDatabaseViewDto> GetDatabaseStatusAsync(CancellationToken cancellationToken = default);
        Task<TechIntegrityResultDto> VerifyIntegrityAsync(string techUser, CancellationToken cancellationToken = default);
        Task<TechOperationResultDto> CreateSafeBackupAsync(
            string techUser,
            string? note = null,
            string? customDirectory = null,
            bool isTest = false,
            CancellationToken cancellationToken = default);
        Task<TechOperationResultDto> DeleteBackupSafelyAsync(string fileName, string techUser, CancellationToken cancellationToken = default);
        Task<TechOperationResultDto> RestoreBackupSafelyAsync(string fileName, string techUser, CancellationToken cancellationToken = default);
        Task<TechOperationResultDto> SaveBackupConfigAsync(TechBackupConfigDto dto, string techUser, CancellationToken cancellationToken = default);
        Task RunScheduledBackupIfDueAsync(CancellationToken cancellationToken = default);
        Task<string> GetConfiguredBackupDirectoryAsync(CancellationToken cancellationToken = default);
        Task<(byte[] Content, string FileName)> ExportTechnicalDbReportJsonAsync(string techUser, CancellationToken cancellationToken = default);

        // 3. Usuarios y Accesos Técnicos
        Task<TechUsersViewDto> GetUsersAndAccessAsync(CancellationToken cancellationToken = default);
        Task<TechOperationResultDto> ToggleUserActiveTechAsync(Guid targetUserId, bool activar, string techUser, CancellationToken cancellationToken = default);
        Task<TechOperationResultDto> ResetUserPasswordTechAsync(Guid targetUserId, string newPassword, string techUser, CancellationToken cancellationToken = default);
        Task RegisterUserLastAccessAsync(Guid usuarioId, CancellationToken cancellationToken = default);

        // 4. Configuración Técnica + Impresión de Prueba
        Task<TechConfigViewDto> GetTechnicalConfigAsync(CancellationToken cancellationToken = default);
        Task<TechOperationResultDto> SaveTechnicalConfigAsync(TechConfigViewDto dto, string techUser, CancellationToken cancellationToken = default);
        Task<string> GenerateTestTicketHtmlAsync(string techUser, CancellationToken cancellationToken = default);

        // 5. Logs y Errores del Sistema
        Task LogEventAsync(
            TipoLogTecnico tipo,
            string severidad,
            string origen,
            string mensaje,
            string? detalleTecnico = null,
            string? usuarioRelacionado = null,
            CancellationToken cancellationToken = default);
        Task RecordAuditAsync(
            string usuarioTecnico,
            string modulo,
            string accion,
            bool exitosa,
            string? detalle = null,
            CancellationToken cancellationToken = default);
        Task<TechLogsFilterDto> QueryLogsAndAuditsAsync(
            DateTime? desdeLocal,
            DateTime? hastaLocal,
            TipoLogTecnico? tipo,
            string? severidad,
            string? busqueda = null,
            CancellationToken cancellationToken = default);
        Task<TechOperationResultDto> PurgeOldLogsAsync(int olderThanDays, string techUser, CancellationToken cancellationToken = default);
        Task<(byte[] Content, string ContentType, string FileName)> ExportLogsAsync(
            string format,
            DateTime? desdeLocal,
            DateTime? hastaLocal,
            TipoLogTecnico? tipo,
            string? severidad,
            string? busqueda,
            string techUser,
            CancellationToken cancellationToken = default);

        // 6. Diagnóstico del Sistema (9 comprobaciones con recomendaciones)
        Task<TechDiagnosticsViewDto> RunFullDiagnosticsAsync(string? techUser = null, CancellationToken cancellationToken = default);
        Task<(byte[] ZipBytes, string FileName)> GenerateDiagnosticPackageZipAsync(string techUser, CancellationToken cancellationToken = default);

        // 7. Historial de Mantenimiento + Preparación de Actualizaciones Inno Setup 6
        Task<TechMaintenanceHistoryViewDto> GetMaintenanceHistoryAsync(
            DateTime? desdeLocal,
            DateTime? hastaLocal,
            TipoMantenimiento? tipo,
            CancellationToken cancellationToken = default);
        Task<TechOperationResultDto> AddMaintenanceRecordAsync(
            CreateMaintenanceRecordDto dto,
            string techUser,
            CancellationToken cancellationToken = default);
        Task<TechOperationResultDto> PrepareInnoSetupUpdateAsync(
            string versionDestino,
            string notas,
            string techUser,
            CancellationToken cancellationToken = default);
    }
}
