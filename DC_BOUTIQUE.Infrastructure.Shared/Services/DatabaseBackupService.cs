using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Infrastructure.Shared.Backup;

namespace DC_BOUTIQUE.Infrastructure.Shared.Services
{
    public class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly DatabaseBackupManager _backupManager;

        public DatabaseBackupService(DatabaseBackupManager backupManager)
        {
            _backupManager = backupManager;
        }

        public async Task<BackupResult> CreateBackupAsync(string? customNote = null, CancellationToken cancellationToken = default)
        {
            return await _backupManager.CreateBackupAsync(customNote, cancellationToken);
        }

        public async Task<List<BackupFileInfo>> GetAvailableBackupsAsync()
        {
            return await _backupManager.GetAvailableBackupsAsync();
        }

        public async Task<bool> DeleteBackupAsync(string fileName)
        {
            return await _backupManager.DeleteBackupAsync(fileName);
        }
    }
}
