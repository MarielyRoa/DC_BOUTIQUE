using DC_BOUTIQUE.Core.Domain.Common;
using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Domain.Entities
{
    /// <summary>
    /// Registro técnico de errores, excepciones, ventas, impresión, persistencia, respaldos y autenticación.
    /// Política de seguridad: jamás almacena contraseñas, PINs, tokens, hashes ni secretos.
    /// </summary>
    public class RegistroLogTecnico : BasicEntity
    {
        public DateTime FechaEvento { get; set; } = DateTime.UtcNow;
        public TipoLogTecnico Tipo { get; set; } = TipoLogTecnico.Error;
        public string Severidad { get; set; } = "Error"; // Info, Warning, Error, Critical
        public string Origen { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? DetalleTecnico { get; set; }
        public string? UsuarioRelacionado { get; set; }
    }

    /// <summary>
    /// Bitácora de auditoría para operaciones técnicas importantes realizadas en el Centro del Desarrollador.
    /// </summary>
    public class AuditoriaTecnica : BasicEntity
    {
        public DateTime FechaAccion { get; set; } = DateTime.UtcNow;
        public string UsuarioTecnico { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public bool Exitosa { get; set; } = true;
        public string? Detalle { get; set; }
    }

    /// <summary>
    /// Configuración persistente de respaldos automáticos y política de retención en SQLite local.
    /// </summary>
    public class ConfiguracionRespaldos : BasicEntity
    {
        public bool AutomaticosActivos { get; set; } = true;
        public FrecuenciaRespaldo Frecuencia { get; set; } = FrecuenciaRespaldo.Cada2Horas;
        public bool RespaldoDiarioActivo { get; set; } = true;
        public string HoraRespaldoDiario { get; set; } = "20:00"; // HH:mm hora local
        public string CarpetaDestino { get; set; } = string.Empty;

        // Políticas de retención recomendadas
        public int RetenerHorarios { get; set; } = 12;
        public int RetenerDiarios { get; set; } = 7;
        public int RetenerSemanales { get; set; } = 4;
        public int RetenerMensuales { get; set; } = 3;
        public int RetencionDias { get; set; } = 90;
        public bool ConservarDiarios { get; set; } = true;
        public bool ConservarSemanales { get; set; } = true;
        public bool ConservarMensuales { get; set; } = true;

        // Seguimiento de ejecución real (resiliencia ante cierre o apagado de Windows)
        public DateTime? UltimoRespaldoExitosoUtc { get; set; }
        public string? UltimoArchivoRespaldo { get; set; }
        public DateTime? UltimoRespaldoDiarioUtc { get; set; }
        public DateTime? ProximoRespaldoProgramadoUtc { get; set; }
        public string? UltimoErrorRespaldo { get; set; }
        public DateTime? FechaUltimoErrorUtc { get; set; }
    }

    /// <summary>
    /// Configuración técnica de aplicación, impresión térmica y parámetros regionales de DC_BOUTIQUE.
    /// </summary>
    public class ConfiguracionTecnica : BasicEntity
    {
        // Impresión
        public string ImpresoraConfigurada { get; set; } = "Predeterminada de Windows";
        public int AnchoPapelMm { get; set; } = 80;
        public string NombreComercialTicket { get; set; } = "DAHIANA CORPORAN BOUTIQUE";
        public string DireccionTicket { get; set; } = "Calle Principal, República Dominicana";
        public string TelefonoTicket { get; set; } = "(809) 000-0000";
        public string MensajePieTicket { get; set; } = "¡Gracias por su compra! Cambios dentro de los 7 días con este ticket.";

        // Regional
        public string Moneda { get; set; } = "DOP";
        public string FormatoFecha { get; set; } = "dd/MM/yyyy";
        public string ZonaHoraria { get; set; } = "America/Santo_Domingo";
        public string CulturaRegional { get; set; } = "es-DO";

        // Versión registrada
        public string VersionRegistrada { get; set; } = "1.0.0";
    }

    /// <summary>
    /// Historial de intervenciones técnicas y mantenimiento realizadas por la desarrolladora (ej. vía AnyDesk o actualizaciones Inno Setup).
    /// </summary>
    public class RegistroMantenimiento : BasicEntity
    {
        public DateTime FechaMantenimientoUtc { get; set; } = DateTime.UtcNow;
        public TipoMantenimiento Tipo { get; set; } = TipoMantenimiento.MantenimientoPreventivo;
        public string DescripcionProblema { get; set; } = string.Empty;
        public string SolucionAplicada { get; set; } = string.Empty;
        public string VersionSistema { get; set; } = "1.0.0";
        public string? VersionAnterior { get; set; }
        public bool RespaldoPrevioRealizado { get; set; }
        public string Resultado { get; set; } = "Exitoso";
        public string? Observaciones { get; set; }
        public string RegistradoPor { get; set; } = string.Empty;
    }
}
