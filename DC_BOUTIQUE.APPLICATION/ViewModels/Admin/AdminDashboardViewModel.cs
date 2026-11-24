namespace DC_BOUTIQUE.Core.Application.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public decimal VentasHoy { get; set; }
        public int CantidadVentasHoy { get; set; }
        public decimal VentasMes { get; set; }
        public int ProductosStockBajo { get; set; }

        public bool HayTurnoCajaAbierto { get; set; }
        public string? CajeroActual { get; set; }
        public decimal EfectivoEnCajaActual { get; set; }

        public List<VentaRecienteItemViewModel> UltimasVentas { get; set; } = new();
        public List<ProductoBajoStockItemViewModel> AlertasStock { get; set; } = new();
    }

    public class VentaRecienteItemViewModel
    {
        public string NumeroFactura { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string MetodosPago { get; set; } = string.Empty;
    }

    public class ProductoBajoStockItemViewModel
    {
        public string NombreProducto { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Talla { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
    }
}
