using DC_BOUTIQUE.Core.Application.DTOs.Caja;
using DC_BOUTIQUE.Core.Application.DTOs.Ventas;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Infrastructure.Shared.Printing;

namespace DC_BOUTIQUE.Infrastructure.Shared.Services
{
    public class TicketPrintingService : ITicketPrintingService
    {
        public string GenerateTicketHtml(VentaDto venta, BoutiqueInfoModel? customInfo = null)
        {
            return TicketHtmlGenerator.GenerateTicketHtml(venta, customInfo);
        }

        public string GenerateCierreCajaTicketHtml(TurnoCajaDto turno, BoutiqueInfoModel? customInfo = null)
        {
            return TicketHtmlGenerator.GenerateCierreCajaTicketHtml(turno, customInfo);
        }

        public byte[] GenerateEscPosTicketBytes(VentaDto venta, BoutiqueInfoModel? customInfo = null)
        {
            return EscPosHelper.GenerateTicketBytes(venta, customInfo);
        }
    }
}
