namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public class BackupFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
        public double SizeInMb => Math.Round((double)SizeInBytes / (1024 * 1024), 2);
        public DateTime CreatedAt { get; set; }
        public string DatabaseType { get; set; } = string.Empty;
    }

    public class BackupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? BackupPath { get; set; }
        public string? FileName { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public interface IDatabaseBackupService
    {
        Task<BackupResult> CreateBackupAsync(string? customNote = null, CancellationToken cancellationToken = default);
        Task<List<BackupFileInfo>> GetAvailableBackupsAsync();
        Task<bool> DeleteBackupAsync(string fileName);
    }
}
