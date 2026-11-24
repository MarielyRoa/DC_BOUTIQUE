using DC_BOUTIQUE.Core.Application.DTOs.Ventas;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Infrastructure.Shared.Helpers;
using System.Text;

namespace DC_BOUTIQUE.Infrastructure.Shared.Printing
{
    /// <summary>
    /// Generador de comandos ESC/POS binarios estándar para impresoras térmicas comerciales (Epson, Bixolon, Xprinter).
    /// </summary>
    public static class EscPosHelper
    {
        // Códigos de control ESC/POS
        private static readonly byte[] EscInit = { 0x1B, 0x40 };          // ESC @ (Reset/Init)
        private static readonly byte[] EscAlignLeft = { 0x1B, 0x61, 0x00 };// ESC a 0
        private static readonly byte[] EscAlignCenter = { 0x1B, 0x61, 0x01 }; // ESC a 1
        private static readonly byte[] EscAlignRight = { 0x1B, 0x61, 0x02 }; // ESC a 2
        private static readonly byte[] EscBoldOn = { 0x1B, 0x45, 0x01 };    // ESC E 1
        private static readonly byte[] EscBoldOff = { 0x1B, 0x45, 0x00 };   // ESC E 0
        private static readonly byte[] EscFeedCut = { 0x1D, 0x56, 0x41, 0x10 }; // GS V 'A' (Feed y corte parcial)
        private static readonly byte[] EscOpenDrawer = { 0x1B, 0x70, 0x00, 0x19, 0xFA }; // Pulso para abrir gaveta de efectivo

        public static byte[] GenerateTicketBytes(VentaDto venta, BoutiqueInfoModel? customInfo = null)
        {
            var info = customInfo ?? new BoutiqueInfoModel();
            using var ms = new MemoryStream();
            var encoding = Encoding.GetEncoding("windows-1252"); // Encoding latino para caracteres con acento/ñ

            void WriteBytes(byte[] bytes) => ms.Write(bytes, 0, bytes.Length);
            void WriteText(string text)
            {
                var bytes = encoding.GetBytes(text);
                ms.Write(bytes, 0, bytes.Length);
            }
            void WriteLine(string text = "")
            {
                WriteText(text + "\n");
            }

            // Inicializar impresora
            WriteBytes(EscInit);

            // Cabecera centrada
            WriteBytes(EscAlignCenter);
            WriteBytes(EscBoldOn);
            WriteLine(info.NombreComercial);
            WriteBytes(EscBoldOff);
            if (!string.IsNullOrWhiteSpace(info.Eslogan)) WriteLine(info.Eslogan);
            WriteLine($"RNC: {info.Rnc}");
            WriteLine(info.Direccion);
            WriteLine($"Tel: {info.Telefono}");
            WriteLine("================================");

            // Metadatos a la izquierda
            WriteBytes(EscAlignLeft);
            WriteLine($"FACTURA: {venta.NumeroFactura}");
            if (!string.IsNullOrWhiteSpace(venta.Ncf)) WriteLine($"NCF: {venta.Ncf}");
            WriteLine($"FECHA: {DateTimeHelper.FormatReceipt(venta.FechaVenta)}");
            WriteLine($"CAJERO: {venta.NombreUsuario}");
            if (!string.IsNullOrWhiteSpace(venta.NombreCliente)) WriteLine($"CLIENTE: {venta.NombreCliente}");
            WriteLine("--------------------------------");

            // Items
            WriteLine("CANT DESCRIPCION           TOTAL");
            WriteLine("--------------------------------");
            foreach (var item in venta.Detalles)
            {
                WriteLine($"{item.NombreProductoSnapshot}");
                string detail = $" {item.Cantidad}x {item.TallaSnapshot}/{item.ColorSnapshot}".PadRight(20);
                string price = CurrencyHelper.FormatPlain(item.SubtotalLinea).PadLeft(11);
                WriteLine($"{detail}{price}");
            }
            WriteLine("--------------------------------");

            // Totales alineados a la derecha
            WriteBytes(EscAlignRight);
            WriteLine($"SUBTOTAL: {CurrencyHelper.FormatRD(venta.Subtotal)}");
            if (venta.DescuentoTotal > 0) WriteLine($"DESCUENTO: -{CurrencyHelper.FormatRD(venta.DescuentoTotal)}");
            if (venta.ImpuestoTotal > 0) WriteLine($"ITBIS: {CurrencyHelper.FormatRD(venta.ImpuestoTotal)}");
            WriteBytes(EscBoldOn);
            WriteLine($"TOTAL: {CurrencyHelper.FormatRD(venta.TotalNeto)}");
            WriteBytes(EscBoldOff);

            // Formas de pago y desglose de cambio
            WriteLine("--------------------------------");
            foreach (var pago in venta.Pagos)
            {
                if (pago.MetodoPago == Core.Domain.Common.Enums.MetodoPago.Efectivo)
                {
                    WriteLine($"EFECTIVO: {CurrencyHelper.FormatRD(pago.Monto)}");
                    if (!string.IsNullOrWhiteSpace(pago.ReferenciaTransaccion) && pago.ReferenciaTransaccion.Contains("RECIBIDO:"))
                    {
                        var parts = pago.ReferenciaTransaccion.Split('|');
                        var recPart = parts.FirstOrDefault(p => p.StartsWith("RECIBIDO:"))?.Replace("RECIBIDO:", "").Trim();
                        var camPart = parts.FirstOrDefault(p => p.StartsWith("CAMBIO:"))?.Replace("CAMBIO:", "").Trim();

                        if (decimal.TryParse(recPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var montoRecibido))
                        {
                            WriteLine($" RECIBIDO: {CurrencyHelper.FormatRD(montoRecibido)}");
                        }
                        if (decimal.TryParse(camPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var montoCambio))
                        {
                            WriteLine($" CAMBIO: {CurrencyHelper.FormatRD(montoCambio)}");
                        }
                    }
                }
                else
                {
                    WriteLine($"{pago.MetodoPago}: {CurrencyHelper.FormatRD(pago.Monto)}");
                }
            }

            // Pie centrado
            WriteBytes(EscAlignCenter);
            WriteLine("--------------------------------");
            WriteLine(info.MensajePie);
            WriteLine("\n\n");

            // Corte de papel
            WriteBytes(EscFeedCut);

            return ms.ToArray();
        }

        public static byte[] GetOpenCashDrawerBytes()
        {
            return EscOpenDrawer;
        }
    }
}
