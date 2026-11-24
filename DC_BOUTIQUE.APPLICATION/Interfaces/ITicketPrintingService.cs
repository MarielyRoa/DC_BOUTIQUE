using DC_BOUTIQUE.Core.Application.DTOs.Caja;
using DC_BOUTIQUE.Core.Application.DTOs.Ventas;

namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public class BoutiqueInfoModel
    {
        public string NombreComercial { get; set; } = "DAHIANA CORPORAN BOUTIQUE";
        public string Eslogan { get; set; } = "Moda, Estilo & Distinción";
        public string Rnc { get; set; } = "131-XXXXX-X";
        public string Direccion { get; set; } = "Calle Principal, República Dominicana";
        public string Telefono { get; set; } = "(809) 000-0000";
        public string Instagram { get; set; } = "@dc_boutiquerd";
        public string MensajePie { get; set; } = "¡Gracias por su compra! Cambios dentro de los 7 días con este ticket.";
        public int AnchoPapelMm { get; set; } = 80; // 80mm o 58mm
    }

    public interface ITicketPrintingService
    {
        string GenerateTicketHtml(VentaDto venta, BoutiqueInfoModel? customInfo = null);
        string GenerateCierreCajaTicketHtml(TurnoCajaDto turno, BoutiqueInfoModel? customInfo = null);
        byte[] GenerateEscPosTicketBytes(VentaDto venta, BoutiqueInfoModel? customInfo = null);
    }
}
