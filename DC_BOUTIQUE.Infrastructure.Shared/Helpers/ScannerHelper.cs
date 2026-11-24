namespace DC_BOUTIQUE.Infrastructure.Shared.Helpers
{
    public enum TipoCodigoEscaneado
    {
        Desconocido = 0,
        SkuProducto = 1,
        CodigoBarrasProducto = 2,
        TicketVenta = 3,
        ComprobanteFiscalNcf = 4,
        QrPagoTransferencia = 5
    }

    public class EscaneoInfo
    {
        public string RawCode { get; set; } = string.Empty;
        public TipoCodigoEscaneado Tipo { get; set; } = TipoCodigoEscaneado.Desconocido;
        public string CleanCode { get; set; } = string.Empty;
        public Dictionary<string, string> Parametros { get; set; } = new();
    }

    /// <summary>
    /// Utilidad para analizar y clasificar tramas leídas por pistolas escáner USB/Bluetooth (1D Barras y 2D QR).
    /// </summary>
    public static class ScannerHelper
    {
        /// <summary>
        /// Limpia y clasifica un código leído por la pistola escáner en el mostrador de la boutique.
        /// </summary>
        public static EscaneoInfo AnalizarCodigo(string? input)
        {
            var info = new EscaneoInfo();
            if (string.IsNullOrWhiteSpace(input)) return info;

            string clean = input.Trim();
            info.RawCode = clean;
            info.CleanCode = clean;

            // 1. ¿Es un comprobante fiscal NCF dominicano (B02..., B01..., E31...)?
            if (DominicanTaxHelper.ValidarFormatoNcf(clean))
            {
                info.Tipo = TipoCodigoEscaneado.ComprobanteFiscalNcf;
                return info;
            }

            // 2. ¿Es un número de factura/ticket de la boutique (ej. FAC-20261118-0001 o TIK-...)?
            if (clean.StartsWith("FAC-", StringComparison.OrdinalIgnoreCase) ||
                clean.StartsWith("TIK-", StringComparison.OrdinalIgnoreCase))
            {
                info.Tipo = TipoCodigoEscaneado.TicketVenta;
                return info;
            }

            // 3. ¿Es un QR de pago/transferencia o URL de pasarela?
            if (clean.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                clean.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                clean.StartsWith("pagorapido:", StringComparison.OrdinalIgnoreCase) ||
                clean.StartsWith("bhd:", StringComparison.OrdinalIgnoreCase) ||
                clean.StartsWith("banreservas:", StringComparison.OrdinalIgnoreCase))
            {
                info.Tipo = TipoCodigoEscaneado.QrPagoTransferencia;
                return info;
            }

            // 4. ¿Es un código de barras EAN-13 / UPC numérico estándar (12 o 13 dígitos)?
            if (clean.Length is 12 or 13 && clean.All(char.IsDigit))
            {
                info.Tipo = TipoCodigoEscaneado.CodigoBarrasProducto;
                return info;
            }

            // 5. En una boutique de ropa, cualquier otro código con guiones o alfanumérico se trata como SKU de prenda
            // (ej: VES-NEG-S, BLU-BLA-M, ACC-001)
            info.Tipo = TipoCodigoEscaneado.SkuProducto;
            return info;
        }
    }
}
