using DC_BOUTIQUE.Core.Domain.Common;

namespace DC_BOUTIQUE.Core.Domain.Entities
{
    public class Producto : BasicEntity
    {
        public required string CodigoModelo { get; set; }
        public required string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Marca { get; set; } // Texto simple: ej. "Zara", "Shein", "Propia"

        public Guid CategoriaId { get; set; }

        public string? MaterialPrincipal { get; set; }
        public string? ImagenUrl { get; set; }

        public decimal CostoBaseReferencia { get; set; }
        public decimal PrecioVentaBase { get; set; }
        public decimal? PrecioOferta { get; set; }
        public decimal PorcentajeImpuesto { get; set; } = 18m; // ITBIS
        public bool AplicaImpuesto { get; set; } = true;

        // Propiedades de navegación
        public Categoria? Categoria { get; set; }
        public ICollection<ProductoVariante> Variantes { get; set; } = new List<ProductoVariante>();
    }
}
