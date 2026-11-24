using System;

namespace DC_BOUTIQUE.Core.Application.Helpers
{
    /// <summary>
    /// Proveedor centralizado de fecha y hora local de República Dominicana (UTC-4 / AST).
    /// </summary>
    public static class DominicanDateTime
    {
        private static readonly TimeZoneInfo DominicanTimeZone;

        static DominicanDateTime()
        {
            try
            {
                // Identificador en Windows
                DominicanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time");
            }
            catch
            {
                try
                {
                    // Identificador IANA (Linux / Mac / Containers)
                    DominicanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo");
                }
                catch
                {
                    // Fallback directo a UTC -4 horas
                    DominicanTimeZone = TimeZoneInfo.CreateCustomTimeZone("AST", TimeSpan.FromHours(-4), "Atlantic Standard Time", "Atlantic Standard Time");
                }
            }
        }

        /// <summary>
        /// Obtiene la fecha y hora actual en República Dominicana.
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, DominicanTimeZone);

        /// <summary>
        /// Convierte una fecha a la hora local dominicana.
        /// </summary>
        public static DateTime ToDominicanTime(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Unspecified)
            {
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(dt.ToUniversalTime(), DominicanTimeZone);
        }
    }
}
