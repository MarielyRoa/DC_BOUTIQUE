using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.DTOs.Productos
{
    public class SaveProductoVarianteDto
    {
        public Guid? Id { get; set; }
        public Guid ProductoId { get; set; }

        [Required(ErrorMessage = "El SKU es obligatorio.")]
        [StringLength(60)]
        public string Sku { get; set; } = string.Empty;

        [StringLength(80)]
        public string? CodigoBarras { get; set; }

        [Required(ErrorMessage = "La talla es obligatoria.")]
        [StringLength(30)]
        public string Talla { get; set; } = string.Empty;

        [Required(ErrorMessage = "El color es obligatorio.")]
        [StringLength(50)]
        public string Color { get; set; } = string.Empty;

        [Range(0, 100000, ErrorMessage = "El stock inicial debe ser mayor o igual a 0.")]
        public int StockActual { get; set; } = 0;

        [Range(0, 1000, ErrorMessage = "El stock mínimo debe ser mayor o igual a 0.")]
        public int StockMinimoAlerta { get; set; } = 2;
    }
}
