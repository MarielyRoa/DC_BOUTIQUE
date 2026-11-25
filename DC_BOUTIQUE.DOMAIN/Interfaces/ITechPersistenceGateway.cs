using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Entities;

namespace DC_BOUTIQUE.Core.Domain.Interfaces
{
    public class SqliteHealthSnapshot
    {
        public bool CanConnect { get; set; }
        public bool IsSqlite { get; set; }
        public string DatabasePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public long WalSizeBytes { get; set; }
        public string SqliteVersion { get; set; } = "N/D";
        public string JournalMode { get; set; } = "N/D";
        public bool ForeignKeysEnabled { get; set; }
        public long PageCount { get; set; }
        public long PageSize { get; set; }
        public long FreelistCount { get; set; }
        public List<string> TablesFound { get; set; } = new();
        public List<string> MissingExpectedTables { get; set; } = new();
        public bool SchemaUpToDate { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class SqliteIntegrityReport
    {
        public bool IsValid { get; set; }
        public List<string> Results { get; set; } = new();
        public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
        public long DurationMs { get; set; }
    }

    public class SqliteRestoreOutcome
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? PreRestoreBackupFileName { get; set; }
        public string? PreRestoreBackupPath { get; set; }
        public bool PostRestoreQueryVerified { get; set; }
    }

    public class SystemCountsSnapshot
    {
        public int TotalUsuarios { get; set; }
        public int UsuariosActivos { get; set; }
        public int TotalProductos { get; set; }
        public int TotalVariantes { get; set; }
    }

    public interface ITechPersistenceGateway
    {
        // SQLite & Diagnóstico
        Task<SqliteHealthSnapshot> GetSqliteHealthAsync(CancellationToken cancellationToken = default);
        Task<SystemCountsSnapshot> GetSystemCountsAsync(CancellationToken cancellationToken = default);
        Task<SqliteIntegrityReport> RunIntegrityCheckAsync(CancellationToken cancellationToken = default);
        Task<SqliteIntegrityReport> VerifySqliteFileIntegrityAsync(string filePath, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message, long BytesWritten)> CreateOnlineSqliteBackupAsync(string destinationFilePath, CancellationToken cancellationToken = default);
        Task<SqliteRestoreOutcome> RestoreSqliteBackupSafelyAsync(string sourceBackupPath, string preRestoreBackupPath, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message)> RunControlledSchemaEvolutionAsync(CancellationToken cancellationToken = default);

        // Logs técnicos (sanitizados)
        Task AddLogAsync(RegistroLogTecnico log, CancellationToken cancellationToken = default);
        Task<List<RegistroLogTecnico>> QueryLogsAsync(
            DateTime? desdeUtc,
            DateTime? hastaUtc,
            TipoLogTecnico? tipo,
            string? severidad,
            string? searchText = null,
            int maxRows = 300,
            CancellationToken cancellationToken = default);
        Task<int> CountLogsAsync(DateTime? desdeUtc = null, TipoLogTecnico? tipo = null, CancellationToken cancellationToken = default);
        Task<int> PurgeOldLogsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

        // Auditoría técnica
        Task AddAuditAsync(AuditoriaTecnica audit, CancellationToken cancellationToken = default);
        Task<List<AuditoriaTecnica>> GetRecentAuditsAsync(int maxRows = 100, CancellationToken cancellationToken = default);

        // Configuración de respaldos automáticos
        Task<ConfiguracionRespaldos> GetBackupConfigAsync(CancellationToken cancellationToken = default);
        Task SaveBackupConfigAsync(ConfiguracionRespaldos config, CancellationToken cancellationToken = default);

        // Configuración técnica del sistema
        Task<ConfiguracionTecnica> GetTechConfigAsync(CancellationToken cancellationToken = default);
        Task SaveTechConfigAsync(ConfiguracionTecnica config, CancellationToken cancellationToken = default);

        // Historial de mantenimiento
        Task AddMaintenanceRecordAsync(RegistroMantenimiento record, CancellationToken cancellationToken = default);
        Task<List<RegistroMantenimiento>> QueryMaintenanceRecordsAsync(
            DateTime? desdeUtc,
            DateTime? hastaUtc,
            TipoMantenimiento? tipo,
            CancellationToken cancellationToken = default);

        // Verificación de relaciones comerciales de usuario (para conservación de historial)
        Task<bool> UserHasCommercialOperationsAsync(Guid usuarioId, CancellationToken cancellationToken = default);
        Task UpdateUserLastAccessAsync(Guid usuarioId, DateTime utcNow, CancellationToken cancellationToken = default);
    }
}
