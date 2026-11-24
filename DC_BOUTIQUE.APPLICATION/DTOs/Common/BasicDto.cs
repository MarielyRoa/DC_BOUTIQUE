namespace DC_BOUTIQUE.Core.Application.DTOs.Common
{
    public abstract class BasicDto
    {
        public Guid Id { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Activo { get; set; }
    }
}
