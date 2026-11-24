namespace DC_BOUTIQUE.Core.Domain.Helpers
{
    /// <summary>
    /// Resuelve de forma dinámica y no cableada (sin rutas absolutas fijas)
    /// la ubicación oficial de la base de datos y respaldos en ProgramData:
    /// C:\ProgramData\DC_BOUTIQUE\Data\DC_BOUTIQUE.db
    /// </summary>
    public static class DatabasePathHelper
    {
        public const string AppFolder = "DC_BOUTIQUE";
        public const string DataFolder = "Data";
        public const string BackupsFolder = "Backups";
        public const string ImagesFolder = "Images";
        public const string DatabaseFileName = "DC_BOUTIQUE.db";

        /// <summary>
        /// Resuelve dinámicamente la carpeta raíz de la aplicación en CommonApplicationData:
        /// En Windows: C:\ProgramData\DC_BOUTIQUE
        /// </summary>
        public static string GetBaseApplicationDataDirectory()
        {
            var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrWhiteSpace(commonAppData))
            {
                commonAppData = @"C:\ProgramData";
            }

            var dir = Path.Combine(commonAppData, AppFolder);
            EnsureDirectoryExists(dir);
            return dir;
        }

        /// <summary>
        /// Resuelve dinámicamente la carpeta de datos:
        /// C:\ProgramData\DC_BOUTIQUE\Data
        /// </summary>
        public static string GetDataDirectory()
        {
            var dir = Path.Combine(GetBaseApplicationDataDirectory(), DataFolder);
            EnsureDirectoryExists(dir);
            return dir;
        }

        /// <summary>
        /// Resuelve dinámicamente la ruta absoluta del archivo de base de datos SQLite:
        /// C:\ProgramData\DC_BOUTIQUE\Data\DC_BOUTIQUE.db
        /// </summary>
        public static string GetDatabaseFilePath()
        {
            return Path.Combine(GetDataDirectory(), DatabaseFileName);
        }

        /// <summary>
        /// Resuelve dinámicamente la carpeta de respaldos independiente:
        /// C:\ProgramData\DC_BOUTIQUE\Backups
        /// </summary>
        public static string GetBackupDirectory()
        {
            var dir = Path.Combine(GetBaseApplicationDataDirectory(), BackupsFolder);
            EnsureDirectoryExists(dir);
            return dir;
        }

        /// <summary>
        /// Resuelve dinámicamente la carpeta de imágenes de prendas (fuera de Program Files):
        /// C:\ProgramData\DC_BOUTIQUE\Images
        /// </summary>
        public static string GetImagesDirectory(string? subfolder = null)
        {
            var dir = string.IsNullOrWhiteSpace(subfolder)
                ? Path.Combine(GetBaseApplicationDataDirectory(), ImagesFolder)
                : Path.Combine(GetBaseApplicationDataDirectory(), ImagesFolder, subfolder);
            EnsureDirectoryExists(dir);
            return dir;
        }

        /// <summary>
        /// Cadena de conexión de SQLite generada dinámicamente.
        /// </summary>
        public static string GetSqliteConnectionString()
        {
            var dbPath = GetDatabaseFilePath();
            return $"Data Source={dbPath};Cache=Shared";
        }

        /// <summary>
        /// Garantiza que el directorio exista en el sistema de archivos de Windows.
        /// </summary>
        public static void EnsureDirectoryExists(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
            catch
            {
                // Silencioso para entornos con políticas de sistema restrictivas
            }
        }
    }
}
