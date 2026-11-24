using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.DTOs.Productos
{
    public class SaveProductoDto
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

        [Range(0, 9999999, ErrorMessage = "El costo debe ser mayor o igual a 0.")]
        public decimal CostoBaseReferencia { get; set; }

        [Required(ErrorMessage = "El precio de venta es obligatorio.")]
        [Range(0.01, 9999999, ErrorMessage = "El precio de venta debe ser mayor a 0.")]
        public decimal PrecioVentaBase { get; set; }

        [Range(0.01, 9999999, ErrorMessage = "El precio de oferta debe ser mayor a 0.")]
        public decimal? PrecioOferta { get; set; }

        public decimal PorcentajeImpuesto { get; set; } = 18m;
        public bool AplicaImpuesto { get; set; } = true;

        public List<SaveProductoVarianteDto> Variantes { get; set; } = new();
    }
}
