using System.Globalization;

namespace DC_BOUTIQUE.Infrastructure.Shared.Helpers
{
    /// <summary>
    /// Utilidades para formateo y conversión monetaria en Pesos Dominicanos (DOP / RD$).
    /// </summary>
    public static class CurrencyHelper
    {
        private static readonly CultureInfo DominicanCulture = new("es-DO");

        /// <summary>
        /// Formatea un monto con símbolo dominicano: RD$ 1,250.00
        /// </summary>
        public static string FormatRD(decimal amount)
        {
            return $"RD$ {amount:N2}";
        }

        /// <summary>
        /// Formatea un monto plano para tickets térmicos: 1,250.00
        /// </summary>
        public static string FormatPlain(decimal amount)
        {
            return amount.ToString("N2", DominicanCulture);
        }

        /// <summary>
        /// Convierte un monto decimal a letras en español dominicano para facturas fiscales formales.
        /// Ejemplo: 1550.50 -> "UN MIL QUINIENTOS CINCUENTA PESOS CON 50/100 M.N."
        /// </summary>
        public static string NumeroALetras(decimal monto)
        {
            long entero = (long)Math.Floor(monto);
            int centavos = (int)Math.Round((monto - entero) * 100);

            string letras = ConvertirGrupo(entero);
            if (string.IsNullOrWhiteSpace(letras)) letras = "CERO";

            return $"{letras.Trim()} PESOS CON {centavos:D2}/100 M.N.".ToUpperInvariant();
        }

        private static string ConvertirGrupo(long numero)
        {
            if (numero == 0) return "CERO";
            if (numero == 100) return "CIEN";

            string[] unidades = { "", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE" };
            string[] decenas = { "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
            string[] diezY = { "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE" };
            string[] veinti = { "VEINTE", "VEINTIUNO", "VEINTIDOS", "VEINTITRES", "VEINTICUATRO", "VEINTICINCO", "VEINTISEIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE" };
            string[] centenas = { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };

            if (numero < 10) return unidades[numero];
            if (numero is >= 10 and <= 19) return diezY[numero - 10];
            if (numero is >= 20 and <= 29) return veinti[numero - 20];
            if (numero is >= 30 and <= 99)
            {
                int u = (int)(numero % 10);
                return u == 0 ? decenas[numero / 10] : $"{decenas[numero / 10]} Y {unidades[u]}";
            }
            if (numero is >= 100 and <= 999)
            {
                int c = (int)(numero / 100);
                long resto = numero % 100;
                return resto == 0 ? centenas[c] : $"{centenas[c]} {ConvertirGrupo(resto)}";
            }
            if (numero is >= 1000 and <= 999999)
            {
                long miles = numero / 1000;
                long resto = numero % 1000;
                string textoMiles = miles == 1 ? "MIL" : $"{ConvertirGrupo(miles)} MIL";
                return resto == 0 ? textoMiles : $"{textoMiles} {ConvertirGrupo(resto)}";
            }
            if (numero is >= 1000000 and <= 999999999)
            {
                long millones = numero / 1000000;
                long resto = numero % 1000000;
                string textoMillones = millones == 1 ? "UN MILLÓN" : $"{ConvertirGrupo(millones)} MILLONES";
                return resto == 0 ? textoMillones : $"{textoMillones} {ConvertirGrupo(resto)}";
            }

            return numero.ToString();
        }
    }
}
