namespace DC_BOUTIQUE.Core.Application.Helpers
{
    /// <summary>
    /// Redirección al helper oficial en Core.Domain
    /// </summary>
    public static class DatabasePathHelper
    {
        public static string GetBaseApplicationDataDirectory() => DC_BOUTIQUE.Core.Domain.Helpers.DatabasePathHelper.GetBaseApplicationDataDirectory();
        public static string GetDataDirectory() => DC_BOUTIQUE.Core.Domain.Helpers.DatabasePathHelper.GetDataDirectory();
        public static string GetDatabaseFilePath() => DC_BOUTIQUE.Core.Domain.Helpers.DatabasePathHelper.GetDatabaseFilePath();
        public static string GetBackupDirectory() => DC_BOUTIQUE.Core.Domain.Helpers.DatabasePathHelper.GetBackupDirectory();
        public static string GetImagesDirectory(string? subfolder = null) => DC_BOUTIQUE.Core.Domain.Helpers.DatabasePathHelper.GetImagesDirectory(subfolder);
        public static string GetSqliteConnectionString() => DC_BOUTIQUE.Core.Domain.Helpers.DatabasePathHelper.GetSqliteConnectionString();
        public static void EnsureDirectoryExists(string directoryPath) => DC_BOUTIQUE.Core.Domain.Helpers.DatabasePathHelper.EnsureDirectoryExists(directoryPath);
    }
}
