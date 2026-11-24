using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DC_BOUTIQUE.Core.Application.ViewModels.Productos
{
    public class SaveProductoViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "El código o modelo es obligatorio.")]
        [StringLength(50)]
        public string CodigoModelo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [StringLength(100)]
        public string? Marca { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una categoría.")]
        public Guid CategoriaId { get; set; }

        [StringLength(100)]
        public string? MaterialPrincipal { get; set; }

        [StringLength(500)]
        public string? ImagenUrl { get; set; }

        [DataType(DataType.Upload)]
        public IFormFile? File { get; set; }

        [Range(0, 9999999, ErrorMessage = "El costo debe ser mayor o igual a 0.")]
        public decimal CostoBaseReferencia { get; set; }

        [Required(ErrorMessage = "El precio de venta es obligatorio.")]
        [Range(0.01, 9999999, ErrorMessage = "El precio de venta debe ser mayor a 0.")]
        public decimal PrecioVentaBase { get; set; }

        [Range(0.01, 9999999, ErrorMessage = "El precio de oferta debe ser mayor a 0.")]
        public decimal? PrecioOferta { get; set; }

        public decimal PorcentajeImpuesto { get; set; } = 18m;
        public bool AplicaImpuesto { get; set; } = true;

        public List<SaveProductoVarianteViewModel> Variantes { get; set; } = new();
    }

    public class SaveProductoVarianteViewModel
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
