using System.Text.RegularExpressions;

namespace DC_BOUTIQUE.Infrastructure.Shared.Helpers
{
    /// <summary>
    /// Utilidades para validación y formato de datos fiscales en República Dominicana (DGII):
    /// Cédula, RNC y Números de Comprobante Fiscal (NCF / e-CF).
    /// </summary>
    public static class DominicanTaxHelper
    {
        /// <summary>
        /// Valida si una cédula dominicana de 11 dígitos es matemáticamente válida mediante el algoritmo de Luhn (módulo 10).
        /// </summary>
        public static bool ValidarCedula(string? cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula)) return false;

            string digits = Regex.Replace(cedula, @"[^\d]", "");
            if (digits.Length != 11) return false;

            int[] weights = { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 };
            int sum = 0;

            for (int i = 0; i < 10; i++)
            {
                int val = (digits[i] - '0') * weights[i];
                if (val >= 10) val = (val / 10) + (val % 10);
                sum += val;
            }

            int digitoVerificadorCalculado = (10 - (sum % 10)) % 10;
            int digitoVerificadorReal = digits[10] - '0';

            return digitoVerificadorCalculado == digitoVerificadorReal;
        }

        /// <summary>
        /// Valida si un RNC dominicano de 9 dígitos es válido (módulo 11).
        /// </summary>
        public static bool ValidarRnc(string? rnc)
        {
            if (string.IsNullOrWhiteSpace(rnc)) return false;

            string digits = Regex.Replace(rnc, @"[^\d]", "");
            if (digits.Length != 9) return false;

            int[] weights = { 7, 9, 8, 6, 5, 4, 3, 2 };
            int sum = 0;

            for (int i = 0; i < 8; i++)
            {
                sum += (digits[i] - '0') * weights[i];
            }

            int resto = sum % 11;
            int digitoCalculado = resto switch
            {
                0 => 2,
                1 => 1,
                _ => 11 - resto
            };

            int digitoReal = digits[8] - '0';
            return digitoCalculado == digitoReal;
        }

        /// <summary>
        /// Valida tanto Cédula como RNC según la cantidad de dígitos ingresada.
        /// </summary>
        public static bool ValidarDocumentoFiscal(string? doc)
        {
            if (string.IsNullOrWhiteSpace(doc)) return false;
            string clean = Regex.Replace(doc, @"[^\d]", "");

            return clean.Length switch
            {
                9 => ValidarRnc(clean),
                11 => ValidarCedula(clean),
                _ => false
            };
        }

        /// <summary>
        /// Formatea un NCF estándar a 11 caracteres (ej. B0200000123).
        /// </summary>
        public static string FormatearNcf(string serie, long secuencia)
        {
            serie = (serie ?? "B02").Trim().ToUpperInvariant();
            return $"{serie}{secuencia:D8}";
        }

        /// <summary>
        /// Valida el formato sintáctico de un NCF estándar (B + 2 dígitos de tipo + 8 dígitos de secuencia)
        /// o un e-CF electrónico (E + 2 dígitos de tipo + 10 dígitos de secuencia).
        /// </summary>
        public static bool ValidarFormatoNcf(string? ncf)
        {
            if (string.IsNullOrWhiteSpace(ncf)) return false;
            string clean = ncf.Trim().ToUpperInvariant();

            // Estándar tradicional: B0100000001 (11 caracteres)
            if (Regex.IsMatch(clean, @"^B\d{10}$")) return true;

            // Facturación electrónica DGII: E310000000001 (13 caracteres)
            if (Regex.IsMatch(clean, @"^E\d{12}$")) return true;

            return false;
        }
    }
}
