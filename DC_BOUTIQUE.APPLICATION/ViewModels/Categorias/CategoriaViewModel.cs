using DC_BOUTIQUE.Core.Application.ViewModels.Common;

namespace DC_BOUTIQUE.Core.Application.ViewModels.Categorias
{
    public class CategoriaViewModel : BasicViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int CantidadProductos { get; set; }
    }
}
