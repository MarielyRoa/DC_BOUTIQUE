using DC_BOUTIQUE.Core.Domain.Common;

namespace DC_BOUTIQUE.Core.Domain.Entities
{
    public class Categoria : BasicEntity
    {
        public required string Nombre { get; set; }
        public string? Descripcion { get; set; }

        // Propiedad de navegación
        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
