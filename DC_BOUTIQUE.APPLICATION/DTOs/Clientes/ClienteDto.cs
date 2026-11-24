using DC_BOUTIQUE.Core.Application.DTOs.Common;

namespace DC_BOUTIQUE.Core.Application.DTOs.Clientes
{
    public class ClienteDto : BasicDto
    {
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
        public string? CedulaORnc { get; set; }
        public string Telefono { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string? Notas { get; set; }
        public int TotalCompras { get; set; }
    }
}
