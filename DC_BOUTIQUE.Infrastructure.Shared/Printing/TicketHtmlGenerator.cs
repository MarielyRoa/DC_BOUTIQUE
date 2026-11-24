using DC_BOUTIQUE.Core.Application.DTOs.Caja;
using DC_BOUTIQUE.Core.Application.DTOs.Ventas;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Infrastructure.Shared.Helpers;
using System.Text;

namespace DC_BOUTIQUE.Infrastructure.Shared.Printing
{
    /// <summary>
    /// Generador de recibos y comprobantes en HTML con formato y estilo CSS para impresoras térmicas de 80mm y 58mm.
    /// Diseñado para visualización web directa, impresión desde el navegador (Ctrl+P / auto-print) o visores de caja.
    /// </summary>
    public static class TicketHtmlGenerator
    {
        public static string GenerateTicketHtml(VentaDto venta, BoutiqueInfoModel? customInfo = null)
        {
            var info = customInfo ?? new BoutiqueInfoModel();
            string barcodeDataUri = BarcodeHelper.GenerateCode128DataUri(venta.NumeroFactura, height: 45, showText: true);
            string qrDataUri = BarcodeHelper.GenerateQrDataUri(venta.NumeroFactura, size: 90);

            StringBuilder sb = new();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html lang=\"es\">");
            sb.Append("<head>");
            sb.Append("<meta charset=\"utf-8\">");
            sb.Append("<title>Ticket de Venta — " + venta.NumeroFactura + "</title>");
            sb.Append("<style>");
            sb.Append(@"
                @page { margin: 0; size: auto; }
                body {
                    font-family: 'Courier New', Courier, monospace, 'Plus Jakarta Sans', sans-serif;
                    font-size: 12px;
                    line-height: 1.35;
                    margin: 0;
                    padding: 8px 12px;
                    background-color: #ffffff;
                    color: #111111;
                    width: " + (info.AnchoPapelMm == 58 ? "54mm" : "76mm") + @";
                }
                .text-center { text-align: center; }
                .text-right { text-align: right; }
                .text-left { text-align: left; }
                .bold { font-weight: bold; }
                .divider {
                    border-top: 1px dashed #444444;
                    margin: 6px 0;
                }
                .double-divider {
                    border-top: 2px solid #111111;
                    border-bottom: 1px solid #111111;
                    height: 2px;
                    margin: 6px 0;
                }
                .header-title {
                    font-size: 14px;
                    font-weight: 700;
                    letter-spacing: 0.5px;
                    text-transform: uppercase;
                }
                .header-subtitle {
                    font-size: 11px;
                    font-style: italic;
                    color: #555555;
                }
                .meta-table, .items-table, .totals-table {
                    width: 100%;
                    border-collapse: collapse;
                }
                .meta-table td {
                    padding: 1px 0;
                    font-size: 11px;
                }
                .items-table th {
                    border-bottom: 1px dashed #222222;
                    padding: 4px 0;
                    font-size: 11px;
                }
                .items-table td {
                    padding: 3px 0;
                    vertical-align: top;
                }
                .item-variant-tag {
                    font-size: 10px;
                    color: #444444;
                }
                .totals-table td {
                    padding: 2px 0;
                }
                .grand-total {
                    font-size: 15px;
                    font-weight: bold;
                }
                .barcode-container {
                    margin-top: 10px;
                    text-align: center;
                }
                .barcode-img {
                    max-width: 90%;
                    height: auto;
                }
                .qr-img {
                    width: 80px;
                    height: 80px;
                    margin: 4px auto;
                }
                .footer-notes {
                    font-size: 10px;
                    text-align: center;
                    margin-top: 8px;
                    color: #333333;
                }
                @media print {
                    body { margin: 0; padding: 4px; }
                    .no-print { display: none; }
                }
            ");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body onload=\"window.print()\">");
            sb.Append("<div class=\"no-print\" style=\"text-align:center;margin-bottom:12px;\"><button onclick=\"window.print()\" style=\"padding:7px 18px;background:#0284c7;color:#fff;border:none;border-radius:6px;cursor:pointer;font-weight:bold;font-size:13px;box-shadow:0 2px 6px rgba(0,0,0,0.15);\">🖨 Imprimir Ticket</button></div>");

            // Cabecera de la Boutique
            sb.Append("<div class=\"text-center\">");
            sb.Append($"<div class=\"header-title\">{info.NombreComercial}</div>");
            if (!string.IsNullOrWhiteSpace(info.Eslogan))
            {
                sb.Append($"<div class=\"header-subtitle\">{info.Eslogan}</div>");
            }
            sb.Append($"<div>RNC: {info.Rnc}</div>");
            sb.Append($"<div>{info.Direccion}</div>");
            sb.Append($"<div>Tel: {info.Telefono}</div>");
            if (!string.IsNullOrWhiteSpace(info.Instagram))
            {
                sb.Append($"<div>IG: {info.Instagram}</div>");
            }
            sb.Append("</div>");

            sb.Append("<div class=\"double-divider\"></div>");

            // Información de la Factura / Comprobante
            sb.Append("<table class=\"meta-table\">");
            sb.Append($"<tr><td class=\"bold\">FACTURA:</td><td class=\"text-right bold\">{venta.NumeroFactura}</td></tr>");
            if (!string.IsNullOrWhiteSpace(venta.Ncf))
            {
                sb.Append($"<tr><td class=\"bold\">NCF:</td><td class=\"text-right bold\">{venta.Ncf}</td></tr>");
            }
            sb.Append($"<tr><td>TIPO:</td><td class=\"text-right\">{venta.TipoComprobante}</td></tr>");
            sb.Append($"<tr><td>FECHA:</td><td class=\"text-right\">{DateTimeHelper.FormatReceipt(venta.FechaVenta)}</td></tr>");
            sb.Append($"<tr><td>CAJERO:</td><td class=\"text-right\">{venta.NombreUsuario}</td></tr>");

            if (!string.IsNullOrWhiteSpace(venta.NombreCliente))
            {
                sb.Append($"<tr><td>CLIENTE:</td><td class=\"text-right\">{venta.NombreCliente}</td></tr>");
            }
            sb.Append("</table>");

            sb.Append("<div class=\"divider\"></div>");

            // Detalle de Prendas / Ropa
            sb.Append("<table class=\"items-table\">");
            sb.Append("<thead>");
            sb.Append("<tr><th class=\"text-left\">CANT/DESCRIPCIÓN</th><th class=\"text-right\">PRECIO</th><th class=\"text-right\">TOTAL</th></tr>");
            sb.Append("</thead>");
            sb.Append("<tbody>");

            foreach (var item in venta.Detalles)
            {
                sb.Append("<tr>");
                sb.Append($"<td colspan=\"3\" class=\"bold\">{item.NombreProductoSnapshot}</td>");
                sb.Append("</tr>");
                sb.Append("<tr>");
                sb.Append($"<td class=\"item-variant-tag\">{item.Cantidad}x | Talla: {item.TallaSnapshot} | Color: {item.ColorSnapshot}</td>");
                sb.Append($"<td class=\"text-right\">{CurrencyHelper.FormatPlain(item.PrecioUnitario)}</td>");
                sb.Append($"<td class=\"text-right bold\">{CurrencyHelper.FormatPlain(item.SubtotalLinea)}</td>");
                sb.Append("</tr>");

                if (item.MontoDescuento > 0)
                {
                    sb.Append("<tr>");
                    sb.Append($"<td colspan=\"3\" class=\"item-variant-tag text-right\" style=\"color:#b8860b;\">Desc. ({item.PorcentajeDescuento:N0}%): -{CurrencyHelper.FormatRD(item.MontoDescuento)}</td>");
                    sb.Append("</tr>");
                }
            }

            sb.Append("</tbody>");
            sb.Append("</table>");

            sb.Append("<div class=\"divider\"></div>");

            // Totales e Impuestos
            sb.Append("<table class=\"totals-table\">");
            sb.Append($"<tr><td>SUBTOTAL:</td><td class=\"text-right\">{CurrencyHelper.FormatRD(venta.Subtotal)}</td></tr>");
            if (venta.DescuentoTotal > 0)
            {
                sb.Append($"<tr><td>DESCUENTO:</td><td class=\"text-right\">-{CurrencyHelper.FormatRD(venta.DescuentoTotal)}</td></tr>");
            }
            if (venta.ImpuestoTotal > 0)
            {
                sb.Append($"<tr><td>ITBIS:</td><td class=\"text-right\">{CurrencyHelper.FormatRD(venta.ImpuestoTotal)}</td></tr>");
            }
            sb.Append($"<tr class=\"grand-total\"><td class=\"bold\">TOTAL:</td><td class=\"text-right bold\">{CurrencyHelper.FormatRD(venta.TotalNeto)}</td></tr>");
            sb.Append("</table>");

            // Monto en letras (para respaldo contable dominicano)
            sb.Append("<div class=\"item-variant-tag text-center\" style=\"margin-top:4px;\">");
            sb.Append($"SON: {CurrencyHelper.NumeroALetras(venta.TotalNeto)}");
            sb.Append("</div>");

            sb.Append("<div class=\"divider\"></div>");

            // Formas de Pago
            sb.Append("<table class=\"meta-table\">");
            sb.Append("<tr><td class=\"bold\" colspan=\"2\">FORMA(S) DE PAGO:</td></tr>");
            foreach (var pago in venta.Pagos)
            {
                if (pago.MetodoPago == Core.Domain.Common.Enums.MetodoPago.Efectivo)
                {
                    sb.Append($"<tr><td>• Efectivo:</td><td class=\"text-right bold\">{CurrencyHelper.FormatRD(pago.Monto)}</td></tr>");

                    if (!string.IsNullOrWhiteSpace(pago.ReferenciaTransaccion) && pago.ReferenciaTransaccion.Contains("RECIBIDO:"))
                    {
                        var parts = pago.ReferenciaTransaccion.Split('|');
                        var recPart = parts.FirstOrDefault(p => p.StartsWith("RECIBIDO:"))?.Replace("RECIBIDO:", "").Trim();
                        var camPart = parts.FirstOrDefault(p => p.StartsWith("CAMBIO:"))?.Replace("CAMBIO:", "").Trim();

                        if (decimal.TryParse(recPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var montoRecibido))
                        {
                            sb.Append($"<tr><td style=\"padding-left:14px;color:#475569;\">Efectivo Recibido:</td><td class=\"text-right\">{CurrencyHelper.FormatRD(montoRecibido)}</td></tr>");
                        }
                        if (decimal.TryParse(camPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var montoCambio))
                        {
                            sb.Append($"<tr><td style=\"padding-left:14px;color:#475569;\">Cambio / Devuelto:</td><td class=\"text-right bold\" style=\"color:#059669;\">{CurrencyHelper.FormatRD(montoCambio)}</td></tr>");
                        }
                    }
                }
                else
                {
                    string refTx = !string.IsNullOrWhiteSpace(pago.ReferenciaTransaccion) && !pago.ReferenciaTransaccion.StartsWith("Pago electrónico") 
                        ? $" (Ref: {pago.ReferenciaTransaccion})" 
                        : "";
                    sb.Append($"<tr><td>• {pago.MetodoPago}{refTx}</td><td class=\"text-right\">{CurrencyHelper.FormatRD(pago.Monto)}</td></tr>");
                }
            }
            sb.Append("</table>");

            // Código de barras del número de factura
            sb.Append("<div class=\"barcode-container\">");
            sb.Append($"<img class=\"barcode-img\" src=\"{barcodeDataUri}\" alt=\"{venta.NumeroFactura}\" />");
            sb.Append("</div>");

            // Pie y Políticas de la Boutique
            sb.Append("<div class=\"footer-notes\">");
            sb.Append($"<div>{info.MensajePie}</div>");
            sb.Append("<div style=\"margin-top:5px;font-style:italic;\">Software Administrativo DC_BOUTIQUE</div>");
            sb.Append("</div>");

            sb.Append("</body>");
            sb.Append("</html>");

            return sb.ToString();
        }

        public static string GenerateCierreCajaTicketHtml(TurnoCajaDto turno, BoutiqueInfoModel? customInfo = null)
        {
            var info = customInfo ?? new BoutiqueInfoModel();

            StringBuilder sb = new();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html lang=\"es\">");
            sb.Append("<head>");
            sb.Append("<meta charset=\"utf-8\">");
            sb.Append("<title>Cierre de Caja — " + turno.CodigoTurno + "</title>");
            sb.Append("<style>");
            sb.Append(@"
                body {
                    font-family: 'Courier New', Courier, monospace;
                    font-size: 12px;
                    line-height: 1.35;
                    margin: 0;
                    padding: 8px 12px;
                    background-color: #ffffff;
                    color: #111111;
                    width: 76mm;
                }
                .text-center { text-align: center; }
                .text-right { text-align: right; }
                .bold { font-weight: bold; }
                .divider { border-top: 1px dashed #444444; margin: 6px 0; }
                .double-divider { border-top: 2px solid #111; border-bottom: 1px solid #111; height: 2px; margin: 6px 0; }
                table { width: 100%; border-collapse: collapse; }
                td { padding: 2px 0; font-size: 11px; }
                .sign-line { border-top: 1px solid #222; margin-top: 30px; text-align: center; font-size: 10px; }
            ");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body onload=\"window.print()\">");
            sb.Append("<div class=\"no-print\" style=\"text-align:center;margin-bottom:12px;\"><button onclick=\"window.print()\" style=\"padding:7px 18px;background:#0284c7;color:#fff;border:none;border-radius:6px;cursor:pointer;font-weight:bold;font-size:13px;box-shadow:0 2px 6px rgba(0,0,0,0.15);\">🖨 Imprimir Ticket</button></div>");

            sb.Append("<div class=\"text-center\">");
            sb.Append($"<div class=\"bold\" style=\"font-size:14px;\">{info.NombreComercial}</div>");
            sb.Append("<div class=\"bold\">COMPROBANTE DE CORTE / ARQUEO DE CAJA</div>");
            sb.Append($"<div>TURNO: {turno.CodigoTurno}</div>");
            sb.Append("</div>");

            sb.Append("<div class=\"double-divider\"></div>");

            sb.Append("<table>");
            sb.Append($"<tr><td>CAJERO:</td><td class=\"text-right bold\">{turno.NombreUsuario}</td></tr>");
            sb.Append($"<tr><td>APERTURA:</td><td class=\"text-right\">{DateTimeHelper.FormatReceipt(turno.FechaApertura)}</td></tr>");
            if (turno.FechaCierre.HasValue)
            {
                sb.Append($"<tr><td>CIERRE:</td><td class=\"text-right\">{DateTimeHelper.FormatReceipt(turno.FechaCierre.Value)}</td></tr>");
            }
            sb.Append($"<tr><td>ESTADO:</td><td class=\"text-right bold\">{turno.Estado}</td></tr>");
            sb.Append("</table>");

            sb.Append("<div class=\"divider\"></div>");

            sb.Append("<table>");
            sb.Append($"<tr><td>FONDO INICIAL EN EFECTIVO:</td><td class=\"text-right\">{CurrencyHelper.FormatRD(turno.FondoInicialEfectivo)}</td></tr>");
            sb.Append($"<tr><td>(+) VENTAS EN EFECTIVO:</td><td class=\"text-right\">{CurrencyHelper.FormatRD(turno.TotalVentasEfectivo)}</td></tr>");
            sb.Append($"<tr><td>(-) GASTOS DE CAJA (EFECTIVO):</td><td class=\"text-right\">-{CurrencyHelper.FormatRD(turno.TotalGastosEfectivo)}</td></tr>");
            sb.Append("<div class=\"divider\"></div>");
            sb.Append($"<tr class=\"bold\"><td>(=) EFECTIVO ESPERADO EN CAJA:</td><td class=\"text-right\">{CurrencyHelper.FormatRD(turno.EfectivoEsperadoEnCaja)}</td></tr>");

            if (turno.EfectivoContadoAlCierre.HasValue)
            {
                sb.Append($"<tr class=\"bold\"><td>EFECTIVO CONTADO FÍSICO:</td><td class=\"text-right\">{CurrencyHelper.FormatRD(turno.EfectivoContadoAlCierre.Value)}</td></tr>");
                decimal dif = turno.DiferenciaArqueo ?? 0;
                string difColor = dif == 0 ? "color:#000;" : (dif > 0 ? "color:green;" : "color:red;");
                string difTexto = dif == 0 ? "CUADRADA" : (dif > 0 ? $"SOBRANTE: +{CurrencyHelper.FormatRD(dif)}" : $"FALTANTE: {CurrencyHelper.FormatRD(dif)}");
                sb.Append($"<tr class=\"bold\" style=\"{difColor}\"><td>DIFERENCIA:</td><td class=\"text-right\">{difTexto}</td></tr>");
            }
            sb.Append("</table>");

            sb.Append("<div class=\"divider\"></div>");

            sb.Append("<table>");
            sb.Append("<tr><td class=\"bold\" colspan=\"2\">OTROS MÉTODOS DE PAGO:</td></tr>");
            sb.Append($"<tr><td>• TARJETAS (POS/VOUCHERS):</td><td class=\"text-right\">{CurrencyHelper.FormatRD(turno.TotalVentasTarjeta)}</td></tr>");
            sb.Append($"<tr><td>• TRANSFERENCIAS BANCARIAS:</td><td class=\"text-right\">{CurrencyHelper.FormatRD(turno.TotalVentasTransferencia)}</td></tr>");
            sb.Append("</table>");

            if (!string.IsNullOrWhiteSpace(turno.Observaciones))
            {
                sb.Append("<div class=\"divider\"></div>");
                sb.Append($"<div style=\"font-size:10px;\"><strong>OBS:</strong> {turno.Observaciones}</div>");
            }

            // Firmas de entrega y recepción
            sb.Append("<div style=\"margin-top:25px;\">");
            sb.Append("<div class=\"sign-line\">FIRMA CAJERO / RESPONSABLE</div>");
            sb.Append("</div>");
            sb.Append("<div style=\"margin-top:20px;\">");
            sb.Append("<div class=\"sign-line\">FIRMA GERENCIA / AUDITORÍA</div>");
            sb.Append("</div>");

            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}
