using DC_BOUTIQUE.Core.Application.DTOs.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace DC_BOUTIQUE.Core.Application.DTOs.Inventario
{
    public class MovimientoInventarioDto : BasicDto
    {
        public Guid ProductoVarianteId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string Talla { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;

        public Guid UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;

        public TipoMovimientoInventario TipoMovimiento { get; set; }
        public string NombreTipoMovimiento => TipoMovimiento.ToString();

        public int Cantidad { get; set; }
        public int StockAnterior { get; set; }
        public int StockPosterior { get; set; }

        public decimal CostoUnitarioReferencia { get; set; }
        public string? DocumentoReferencia { get; set; }
        public string? Motivo { get; set; }
    }

    /// <summary>
    /// DTO para la entrada rápida de mercancía que solicita la dueña de la boutique
    /// (sin pasar por órdenes de compra complejas).
    /// </summary>
    public class EntradaMercanciaRapidaDto
    {
        [Required(ErrorMessage = "Debe seleccionar una variante de producto.")]
        public Guid ProductoVarianteId { get; set; }

        [Required(ErrorMessage = "Debe indicar la cantidad recibida.")]
        [Range(1, 100000, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public int Cantidad { get; set; }

        [StringLength(300)]
        public string? Motivo { get; set; } = "Entrada de mercancía / Reabastecimiento";

        [StringLength(80)]
        public string? DocumentoReferencia { get; set; }
    }

    public class AjusteInventarioDto
    {
        [Required(ErrorMessage = "Debe seleccionar una variante de producto.")]
        public Guid ProductoVarianteId { get; set; }

        [Required(ErrorMessage = "Debe indicar el nuevo stock real contado.")]
        [Range(0, 100000, ErrorMessage = "El stock debe ser mayor o igual a 0.")]
        public int StockRealContado { get; set; }

        [Required(ErrorMessage = "Debe ingresar el motivo del ajuste.")]
        [StringLength(300)]
        public string Motivo { get; set; } = string.Empty;
    }
}
