namespace DC_BOUTIQUE.Core.Application.ViewModels.Common
{
    public abstract class BasicViewModel
    {
        public Guid Id { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Activo { get; set; }
    }
}
