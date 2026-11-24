namespace DC_BOUTIQUE.Infrastructure.Shared.Helpers
{
    /// <summary>
    /// Utilidades para el manejo de fechas y zona horaria de República Dominicana (UTC-4 / AST).
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo DominicanTimeZone;

        static DateTimeHelper()
        {
            try
            {
                // Windows timezone ID
                DominicanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time");
            }
            catch
            {
                try
                {
                    // IANA timezone ID (Linux / Mac)
                    DominicanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo");
                }
                catch
                {
                    // Fallback directo a UTC -4 horas sin horario de verano
                    DominicanTimeZone = TimeZoneInfo.CreateCustomTimeZone("AST", TimeSpan.FromHours(-4), "Atlantic Standard Time", "Atlantic Standard Time");
                }
            }
        }

        /// <summary>
        /// Obtiene la hora actual en República Dominicana.
        /// </summary>
        public static DateTime NowDominican => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, DominicanTimeZone);

        /// <summary>
        /// Convierte una fecha UTC a hora dominicana.
        /// </summary>
        public static DateTime ToDominicanTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime.ToUniversalTime(), DominicanTimeZone);
        }

        /// <summary>
        /// Formatea para tickets: dd/MM/yyyy hh:mm tt (ej. 18/11/2026 02:30 PM)
        /// </summary>
        public static string FormatReceipt(DateTime dt)
        {
            var local = dt.Kind == DateTimeKind.Utc ? ToDominicanTime(dt) : dt;
            return local.ToString("dd/MM/yyyy hh:mm tt");
        }
    }
}
