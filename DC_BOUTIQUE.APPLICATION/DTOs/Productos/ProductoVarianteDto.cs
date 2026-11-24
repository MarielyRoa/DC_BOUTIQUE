using DC_BOUTIQUE.Core.Application.DTOs.Common;

namespace DC_BOUTIQUE.Core.Application.DTOs.Productos
{
    public class ProductoVarianteDto : BasicDto
    {
        public Guid ProductoId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? CodigoBarras { get; set; }
        public string Talla { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int StockActual { get; set; }
        public int StockMinimoAlerta { get; set; }
        public Guid ConcurrencyToken { get; set; }

        // Datos del producto padre
        public string NombreProducto { get; set; } = string.Empty;
        public string CodigoModelo { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public decimal? PrecioOferta { get; set; }
        public decimal PrecioEfectivo => (PrecioOferta.HasValue && PrecioOferta.Value > 0) ? PrecioOferta.Value : PrecioVenta;
    }
}
