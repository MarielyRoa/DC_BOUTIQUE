using DC_BOUTIQUE.Core.Domain.Common;

namespace DC_BOUTIQUE.Core.Domain.Entities
{
    public class Cliente : BasicEntity
    {
        public required string Nombres { get; set; }
        public required string Apellidos { get; set; }
        public string? CedulaORnc { get; set; }
        public required string Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string? Notas { get; set; }

        // Propiedad de navegación limpia
        public ICollection<Venta> ComprasRealizadas { get; set; } = new List<Venta>();
    }
}
