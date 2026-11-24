using Microsoft.AspNetCore.Http;

namespace DC_BOUTIQUE.Core.Application.Helpers
{
    /// <summary>
    /// Helper para gestión de subida, actualización y eliminación de archivos e imágenes de la boutique.
    /// Sigue la convención estándar wwwroot/images/{folder}/{id}/{filename}
    /// </summary>
    public static class UploadFileHelper
    {
        /// <summary>
        /// Sube o reemplaza una imagen de producto/ropa de forma asíncrona.
        /// </summary>
        public static async Task<string> UploadFileAsync(IFormFile? file, string id, string folder = "productos", bool isEditMode = false, string? currentImagePath = "", CancellationToken cancellationToken = default)
        {
            if (isEditMode && file == null)
            {
                return currentImagePath ?? string.Empty;
            }

            if (file == null || file.Length == 0)
            {
                return string.Empty;
            }

            // Validar restricciones de ropa / formato
            if (!ClothingImageConfig.ValidateImage(file, out _))
            {
                return currentImagePath ?? string.Empty;
            }

            // Construir ruta física en ProgramData\DC_BOUTIQUE\Images\{folder}\{id} (fuera de Program Files)
            string physicalPath = Path.Combine(DatabasePathHelper.GetImagesDirectory(folder), id);

            if (!Directory.Exists(physicalPath))
            {
                Directory.CreateDirectory(physicalPath);
            }

            // Generar nombre de archivo único con GUID
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string fileName = $"{Guid.NewGuid()}{extension}";
            string fullFilePath = Path.Combine(physicalPath, fileName);

            // Guardar archivo en disco de forma asíncrona
            await using (var stream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // En modo edición, borrar la imagen previa anterior para no saturar el disco
            if (isEditMode && !string.IsNullOrWhiteSpace(currentImagePath))
            {
                try
                {
                    string oldFileName = Path.GetFileName(currentImagePath);
                    string oldPhysicalFile = Path.Combine(physicalPath, oldFileName);

                    if (File.Exists(oldPhysicalFile))
                    {
                        File.Delete(oldPhysicalFile);
                    }
                }
                catch
                {
                    // No bloquear la operación si la imagen previa ya no existía
                }
            }

            // Retornar ruta web relativa accesible desde el navegador
            return $"/images/{folder}/{id}/{fileName}";
        }

        public static async Task<string> UploadFileAsync(IFormFile? file, Guid id, string folder = "productos", bool isEditMode = false, string? currentImagePath = "", CancellationToken cancellationToken = default)
        {
            return await UploadFileAsync(file, id.ToString(), folder, isEditMode, currentImagePath, cancellationToken);
        }

        /// <summary>
        /// Sube o reemplaza una imagen de producto/ropa.
        /// </summary>
        /// <param name="file">Archivo enviado desde el formulario (IFormFile)</param>
        /// <param name="id">Identificador único de la entidad (Producto, Categoría, etc.)</param>
        /// <param name="folder">Carpeta destino dentro de images (ej. 'productos', 'categorias')</param>
        /// <param name="isEditMode">Indica si es modo edición</param>
        /// <param name="currentImagePath">Ruta actual de la imagen en base de datos si está en edición</param>
        /// <returns>Ruta relativa para almacenar en base de datos (ej. /images/productos/guid/filename.ext)</returns>
        public static string UploadFile(IFormFile? file, string id, string folder = "productos", bool isEditMode = false, string? currentImagePath = "")
        {
            if (isEditMode && file == null)
            {
                return currentImagePath ?? string.Empty;
            }

            if (file == null || file.Length == 0)
            {
                return string.Empty;
            }

            // Validar restricciones de ropa / formato
            if (!ClothingImageConfig.ValidateImage(file, out _))
            {
                return currentImagePath ?? string.Empty;
            }

            // Construir ruta física en ProgramData\DC_BOUTIQUE\Images\{folder}\{id} (fuera de Program Files)
            string physicalPath = Path.Combine(DatabasePathHelper.GetImagesDirectory(folder), id);

            if (!Directory.Exists(physicalPath))
            {
                Directory.CreateDirectory(physicalPath);
            }

            // Generar nombre de archivo único con GUID
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string fileName = $"{Guid.NewGuid()}{extension}";
            string fullFilePath = Path.Combine(physicalPath, fileName);

            // Guardar archivo en disco
            using (var stream = new FileStream(fullFilePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // En modo edición, borrar la imagen previa anterior para no saturar el disco
            if (isEditMode && !string.IsNullOrWhiteSpace(currentImagePath))
            {
                try
                {
                    string oldFileName = Path.GetFileName(currentImagePath);
                    string oldPhysicalFile = Path.Combine(physicalPath, oldFileName);

                    if (File.Exists(oldPhysicalFile))
                    {
                        File.Delete(oldPhysicalFile);
                    }
                }
                catch
                {
                    // No bloquear la operación si la imagen previa ya no existía
                }
            }

            // Retornar ruta web relativa accesible desde el navegador
            return $"/images/{folder}/{id}/{fileName}";
        }

        /// <summary>
        /// Sobrecarga para IDs de tipo Guid.
        /// </summary>
        public static string UploadFile(IFormFile? file, Guid id, string folder = "productos", bool isEditMode = false, string? currentImagePath = "")
        {
            return UploadFile(file, id.ToString(), folder, isEditMode, currentImagePath);
        }

        /// <summary>
        /// Elimina físicamente un archivo a partir de su ruta relativa almacenada en base de datos.
        /// </summary>
        public static void DeleteFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            try
            {
                string cleanPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                if (cleanPath.StartsWith($"images{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    string subPath = cleanPath.Substring(7);
                    string programDataPath = Path.Combine(DatabasePathHelper.GetImagesDirectory(), subPath);
                    if (File.Exists(programDataPath))
                    {
                        File.Delete(programDataPath);
                    }
                }

                string legacyPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", cleanPath.StartsWith("wwwroot") ? cleanPath.Substring(7).TrimStart(Path.DirectorySeparatorChar) : cleanPath);
                if (File.Exists(legacyPath))
                {
                    File.Delete(legacyPath);
                }
            }
            catch
            {
                // Manejo silencioso en borrado de archivos no críticos
            }
        }

        /// <summary>
        /// Elimina la carpeta completa asociada a una entidad cuando se borra el producto.
        /// </summary>
        public static void DeleteDirectory(string id, string folder = "productos")
        {
            try
            {
                string programDataDir = Path.Combine(DatabasePathHelper.GetImagesDirectory(folder), id);
                if (Directory.Exists(programDataDir))
                {
                    Directory.Delete(programDataDir, recursive: true);
                }

                string legacyDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", folder, id);
                if (Directory.Exists(legacyDir))
                {
                    Directory.Delete(legacyDir, recursive: true);
                }
            }
            catch
            {
                // Silencioso
            }
        }

        /// <summary>
        /// Sobrecarga para IDs tipo Guid.
        /// </summary>
        public static void DeleteDirectory(Guid id, string folder = "productos")
        {
            DeleteDirectory(id.ToString(), folder);
        }
    }
}
