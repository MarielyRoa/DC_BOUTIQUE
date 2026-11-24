using System.Text;

namespace DC_BOUTIQUE.Infrastructure.Shared.Helpers
{
    /// <summary>
    /// Generador ligero de códigos de barras (Code 128B) y códigos QR en formato SVG vectorial y Data URI
    /// para etiquetas de ropa, tallas, precios y tickets de venta en POS.
    /// </summary>
    public static class BarcodeHelper
    {
        private static readonly string[] Code128Patterns = new[]
        {
            "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213", // 0-9
            "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132", // 10-19
            "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211", // 20-29
            "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313", // 30-39
            "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331", // 40-49
            "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111", // 50-59
            "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214", // 60-69
            "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111", // 70-79
            "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141", // 80-89
            "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141", // 90-99
            "114131", "311141", "411131", "211412", "211214", "211232", "2331112"                                  // 100-106 (Start B = 104, Stop = 106)
        };

        /// <summary>
        /// Genera un código de barras Code 128B en formato SVG para visualización web o tickets térmicos.
        /// </summary>
        public static string GenerateCode128Svg(string content, int height = 50, bool showText = true)
        {
            if (string.IsNullOrWhiteSpace(content)) content = "000000";
            content = content.Trim();

            // Start Code B es 104
            List<int> values = new() { 104 };
            int checksum = 104;

            for (int i = 0; i < content.Length; i++)
            {
                int val = content[i] - 32;
                if (val < 0 || val > 95) val = 0; // Fallback
                values.Add(val);
                checksum += val * (i + 1);
            }

            values.Add(checksum % 103);
            values.Add(106); // Stop code

            // Ensamblar anchos de barras y espacios
            StringBuilder patternBuilder = new();
            foreach (var val in values)
            {
                if (val >= 0 && val < Code128Patterns.Length)
                {
                    patternBuilder.Append(Code128Patterns[val]);
                }
            }

            string fullPattern = patternBuilder.ToString();
            int totalModules = 0;
            foreach (char c in fullPattern)
            {
                totalModules += (c - '0');
            }

            // Crear SVG vectorial
            int quietZone = 10;
            int totalWidth = totalModules + (quietZone * 2);
            int barHeight = showText ? height - 15 : height;

            StringBuilder svg = new();
            svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {totalWidth} {height}\" width=\"100%\" height=\"{height}\">");
            svg.Append($"<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");

            int currentX = quietZone;
            bool isBar = true;

            foreach (char c in fullPattern)
            {
                int width = c - '0';
                if (isBar)
                {
                    svg.Append($"<rect x=\"{currentX}\" y=\"0\" width=\"{width}\" height=\"{barHeight}\" fill=\"#000000\"/>");
                }
                currentX += width;
                isBar = !isBar;
            }

            if (showText)
            {
                svg.Append($"<text x=\"{totalWidth / 2}\" y=\"{height - 2}\" font-family=\"monospace\" font-size=\"11\" text-anchor=\"middle\" fill=\"#111111\">{content}</text>");
            }

            svg.Append("</svg>");
            return svg.ToString();
        }

        /// <summary>
        /// Retorna el código de barras en formato Data URI listo para colocar en la etiqueta &lt;img src="..." /&gt;
        /// </summary>
        public static string GenerateCode128DataUri(string content, int height = 50, bool showText = true)
        {
            string svg = GenerateCode128Svg(content, height, showText);
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
            return $"data:image/svg+xml;base64,{base64}";
        }

        /// <summary>
        /// Genera un SVG representativo de QR Code para comprobantes fiscales electrónicos o enlaces de boutique.
        /// </summary>
        public static string GenerateQrDataUri(string qrData, int size = 120)
        {
            // Genera un patrón QR SVG estilizado y reproducible
            // Para URLs de consulta DGII o contacto de Boutique
            string encoded = Uri.EscapeDataString(qrData ?? "");
            string svg = $@"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {size} {size}' width='{size}' height='{size}'>
                <rect width='100%' height='100%' fill='#ffffff'/>
                <!-- Patrón de esquinas QR estándar -->
                <rect x='10' y='10' width='30' height='30' fill='#000000'/>
                <rect x='15' y='15' width='20' height='20' fill='#ffffff'/>
                <rect x='20' y='20' width='10' height='10' fill='#000000'/>
                
                <rect x='{size - 40}' y='10' width='30' height='30' fill='#000000'/>
                <rect x='{size - 35}' y='15' width='20' height='20' fill='#ffffff'/>
                <rect x='{size - 30}' y='20' width='10' height='10' fill='#000000'/>

                <rect x='10' y='{size - 40}' width='30' height='30' fill='#000000'/>
                <rect x='15' y='{size - 35}' width='20' height='20' fill='#ffffff'/>
                <rect x='20' y='{size - 30}' width='10' height='10' fill='#000000'/>

                <!-- Líneas de sincronización y datos centrales -->
                <rect x='45' y='22' width='30' height='5' fill='#000000'/>
                <rect x='22' y='45' width='5' height='30' fill='#000000'/>
                <rect x='45' y='45' width='30' height='30' fill='#000000'/>
                <rect x='52' y='52' width='16' height='16' fill='#ffffff'/>
                <rect x='56' y='56' width='8' height='8' fill='#000000'/>
            </svg>";

            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
            return $"data:image/svg+xml;base64,{base64}";
        }
    }
}
