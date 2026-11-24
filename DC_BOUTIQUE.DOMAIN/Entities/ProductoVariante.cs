using DC_BOUTIQUE.Core.Domain.Common;

namespace DC_BOUTIQUE.Core.Domain.Entities
{
    /// <summary>
    /// Matriz de Talla y Color de la prenda o calzado con control de stock y código de barras.
    /// </summary>
    public class ProductoVariante : BasicEntity
    {
        public Guid ProductoId { get; set; }

        public required string Sku { get; set; }
        public string? CodigoBarras { get; set; }

        public required string Talla { get; set; } // XS, S, M, L, XL, 37, 38, etc.
        public required string Color { get; set; } // Negro, Blanco, Champagne, etc.

        public int StockActual { get; set; } = 0;
        public int StockMinimoAlerta { get; set; } = 2;

        /// <summary>
        /// Token de concurrencia optimista compatible con SQLite y SQL Server.
        /// Se actualiza con Guid.NewGuid() en cada modificación de stock.
        /// </summary>
        public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

        // Propiedades de navegación
        public Producto? Producto { get; set; }
        public ICollection<DetalleVenta> DetallesVenta { get; set; } = new List<DetalleVenta>();
        public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
    }
}
