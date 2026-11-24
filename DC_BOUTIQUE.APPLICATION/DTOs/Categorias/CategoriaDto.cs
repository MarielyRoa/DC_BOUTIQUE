using DC_BOUTIQUE.Core.Application.DTOs.Common;

namespace DC_BOUTIQUE.Core.Application.DTOs.Categorias
{
    public class CategoriaDto : BasicDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int CantidadProductos { get; set; }
    }
}
