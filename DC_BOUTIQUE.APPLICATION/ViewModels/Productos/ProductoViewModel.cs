using DC_BOUTIQUE.Core.Application.ViewModels.Common;

namespace DC_BOUTIQUE.Core.Application.ViewModels.Productos
{
    public class ProductoVarianteViewModel : BasicViewModel
    {
        public Guid ProductoId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? CodigoBarras { get; set; }
        public string Talla { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int StockActual { get; set; }
        public int StockMinimoAlerta { get; set; }
        public Guid ConcurrencyToken { get; set; }

        public string NombreProducto { get; set; } = string.Empty;
        public string CodigoModelo { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public decimal? PrecioOferta { get; set; }
    }

    public class ProductoViewModel : BasicViewModel
    {
        public string CodigoModelo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Marca { get; set; }
        public Guid CategoriaId { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;
        public string? MaterialPrincipal { get; set; }
        public string? ImagenUrl { get; set; }
        public decimal CostoBaseReferencia { get; set; }
        public decimal PrecioVentaBase { get; set; }
        public decimal? PrecioOferta { get; set; }
        public decimal PorcentajeImpuesto { get; set; }
        public bool AplicaImpuesto { get; set; }

        public int StockTotal => Variantes.Sum(v => v.StockActual);
        public List<ProductoVarianteViewModel> Variantes { get; set; } = new();
    }
}
