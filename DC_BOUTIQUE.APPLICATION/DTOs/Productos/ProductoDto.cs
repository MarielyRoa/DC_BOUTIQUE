using DC_BOUTIQUE.Core.Application.DTOs.Common;

namespace DC_BOUTIQUE.Core.Application.DTOs.Productos
{
    public class ProductoDto : BasicDto
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
        public List<ProductoVarianteDto> Variantes { get; set; } = new();
    }
}
