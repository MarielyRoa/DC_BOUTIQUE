using DC_BOUTIQUE.Core.Domain.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Domain.Entities
{
    public class MovimientoInventario : BasicEntity
    {
        public Guid ProductoVarianteId { get; set; }
        public Guid UsuarioId { get; set; }

        public TipoMovimientoInventario TipoMovimiento { get; set; }
        public int Cantidad { get; set; }
        public int StockAnterior { get; set; }
        public int StockPosterior { get; set; }

        public decimal CostoUnitarioReferencia { get; set; }
        public string? DocumentoReferencia { get; set; }
        public string? Motivo { get; set; }

        // Propiedades de navegación
        public ProductoVariante? ProductoVariante { get; set; }
        public Usuario? Usuario { get; set; }
    }
}
