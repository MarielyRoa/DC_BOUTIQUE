using Microsoft.AspNetCore.Http;

namespace DC_BOUTIQUE.Core.Application.Helpers
{
    /// <summary>
    /// Configuración y especificaciones de tamaños y validaciones para imágenes de ropa y accesorios de boutique.
    /// </summary>
    public static class ClothingImageConfig
    {
        // -------------------------------------------------------------
        // Tamaños recomendados para catálogo de moda y punto de venta
        // -------------------------------------------------------------

        /// <summary>
        /// Miniatura para POS (Punto de Venta) y carrito: 150x150 píxeles (Aspecto 1:1)
        /// </summary>
        public static readonly (int Width, int Height) PosThumbnail = (150, 150);

        /// <summary>
        /// Tarjeta de catálogo / Lookbook: 400x500 píxeles (Proporción vertical de moda 4:5)
        /// </summary>
        public static readonly (int Width, int Height) CatalogCard = (400, 500);

        /// <summary>
        /// Vista detallada / Zoom de tela y acabados: 800x1000 píxeles (4:5 alta definición)
        /// </summary>
        public static readonly (int Width, int Height) DetailView = (800, 1000);

        // -------------------------------------------------------------
        // Restricciones de carga
        // -------------------------------------------------------------

        /// <summary>
        /// Límite máximo de peso de imagen: 5 MB
        /// </summary>
        public const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB

        /// <summary>
        /// Extensiones permitidas para ropa y accesorios
        /// </summary>
        public static readonly string[] AllowedExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".jfif"
        };

        /// <summary>
        /// Tipos MIME permitidos
        /// </summary>
        public static readonly string[] AllowedMimeTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        // -------------------------------------------------------------
        // Placeholders por defecto para boutique
        // -------------------------------------------------------------
        public const string DefaultProductImage = "/images/defaults/product-placeholder.png";
        public const string DefaultCategoryDress = "/images/defaults/dress-placeholder.png";
        public const string DefaultCategoryAccessory = "/images/defaults/accessory-placeholder.png";
        public const string DefaultCategoryFootwear = "/images/defaults/footwear-placeholder.png";

        /// <summary>
        /// Valida si el archivo subido cumple con el tamaño y formato permitido.
        /// </summary>
        public static bool ValidateImage(IFormFile? file, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file == null || file.Length == 0)
            {
                return true; // Archivo opcional
            }

            if (file.Length > MaxFileSizeInBytes)
            {
                errorMessage = "La imagen no debe superar los 5 MB de tamaño.";
                return false;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                errorMessage = $"El formato '{extension}' no es válido. Solo se admiten: {string.Join(", ", AllowedExtensions)}.";
                return false;
            }

            return true;
        }
    }
}
