using DC_BOUTIQUE.Core.Domain.Common.Enums;

namespace DC_BOUTIQUE.Core.Application.DTOs.TechAdmin
{
    public class ServiceHealthItemDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "Correcto"; // Correcto, Advertencia, Error
        public bool Saludable { get; set; }
        public string Detalle { get; set; } = string.Empty;
    }

    public class TechDashboardDto
    {
        public string NombreSistema { get; set; } = "DC_BOUTIQUE";
        public string VersionInstalada { get; set; } = "1.0.0";
        public string EstadoAplicacion { get; set; } = "Correcto";
        public string EstadoGeneral { get; set; } = "Correcto"; // Correcto, Advertencia, Error
        public bool SistemaSaludable { get; set; }

        // SQLite
        public bool SqliteConectada { get; set; }
        public string SqliteVersion { get; set; } = "N/D";
        public string JournalMode { get; set; } = "N/D";
        public bool ForeignKeysEnabled { get; set; }
        public string RutaBaseDatos { get; set; } = string.Empty;
        public long TamanoBaseDatosBytes { get; set; }
        public long TamanoWalBytes { get; set; }

        // Disco
        public string UnidadDisco { get; set; } = "C:\\";
        public long EspacioLibreBytes { get; set; }
        public long EspacioTotalBytes { get; set; }
        public double PorcentajeUsoDisco { get; set; }

        // Respaldos
        public DateTime? FechaUltimoRespaldoExitoso { get; set; }
        public string? NombreUltimoRespaldo { get; set; }
        public DateTime? ProximoRespaldoProgramado { get; set; }
        public bool RespaldosAutomaticosActivos { get; set; }
        public string FrecuenciaRespaldosTexto { get; set; } = "Cada 2 horas";
        public int TotalRespaldosDisponibles { get; set; }

        // Conteos reales de instalación
        public int TotalUsuariosRegistrados { get; set; }
        public int UsuariosActivos { get; set; }
        public int TotalProductos { get; set; }
        public int TotalVariantes { get; set; }

        // Esquema y errores recientes
        public bool EsquemaActualizado { get; set; }
        public int TotalTablasDetectadas { get; set; }
        public List<string> ElementosFaltantesEsquema { get; set; } = new();
        public int ErroresUltimas24Horas { get; set; }
        public int FallosAuthUltimas24Horas { get; set; }
        public List<RegistroLogTecnicoDto> UltimosErrores { get; set; } = new();
        public List<ServiceHealthItemDto> ServiciosPrincipales { get; set; } = new();
    }

    public class TechBackupConfigDto
    {
        public Guid Id { get; set; }
        public bool AutomaticosActivos { get; set; } = true;
        public FrecuenciaRespaldo Frecuencia { get; set; } = FrecuenciaRespaldo.Cada2Horas;
        public bool RespaldoDiarioActivo { get; set; } = true;
        public string HoraRespaldoDiario { get; set; } = "20:00";
        public string CarpetaDestino { get; set; } = string.Empty;

        public int RetenerHorarios { get; set; } = 12;
        public int RetenerDiarios { get; set; } = 7;
        public int RetenerSemanales { get; set; } = 4;
        public int RetenerMensuales { get; set; } = 3;
        public int RetencionDias { get; set; } = 90;
        public bool ConservarDiarios { get; set; } = true;
        public bool ConservarSemanales { get; set; } = true;
        public bool ConservarMensuales { get; set; } = true;

        public DateTime? UltimoRespaldoExitosoLocal { get; set; }
        public string? UltimoArchivoRespaldo { get; set; }
        public DateTime? ProximoRespaldoProgramadoLocal { get; set; }
        public string? UltimoErrorRespaldo { get; set; }
        public DateTime? FechaUltimoErrorLocal { get; set; }
    }

    public class TechBackupEntryDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TipoOrigen { get; set; } = "Manual"; // Manual, Automático, Diario, Preventivo, Prueba
        public string Estado { get; set; } = "Válido";
        public bool EsPreventivo { get; set; }
        public bool EsUltimoValido { get; set; }
    }

    public class TechDatabaseViewDto
    {
        public string RutaBaseDatos { get; set; } = string.Empty;
        public string RutaRespaldos { get; set; } = string.Empty;
        public long TamanoBaseDatosBytes { get; set; }
        public long TamanoWalBytes { get; set; }
        public string SqliteVersion { get; set; } = "N/D";
        public string JournalMode { get; set; } = "N/D";
        public bool ForeignKeysEnabled { get; set; }
        public long PageCount { get; set; }
        public long PageSize { get; set; }
        public long FreelistCount { get; set; }
        public bool EsquemaActualizado { get; set; }
        public List<string> TablasDetectadas { get; set; } = new();
        public List<TechBackupEntryDto> Respaldos { get; set; } = new();
        public TechBackupConfigDto ConfiguracionRespaldos { get; set; } = new();
        public List<RegistroLogTecnicoDto> ErroresRespaldosRecientes { get; set; } = new();
    }

    public class TechIntegrityResultDto
    {
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; } = new();
        public DateTime CheckedAtLocal { get; set; }
        public long DurationMs { get; set; }
    }

    public class TechOperationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? SecondaryDetail { get; set; }
    }

    public class TechUserRowDto
    {
        public Guid Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public RolUsuario Rol { get; set; }
        public bool EsPrincipal { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacionLocal { get; set; }
        public DateTime? UltimoAccesoLocal { get; set; }
        public bool TieneOperacionesComerciales { get; set; }
    }

    public class TechUsersViewDto
    {
        public List<TechUserRowDto> Usuarios { get; set; } = new();
        public List<RegistroLogTecnicoDto> IntentosFallidos { get; set; } = new();
        public List<AuditoriaTecnicaDto> EventosAcceso { get; set; } = new();
    }

    public class TechConfigViewDto
    {
        public Guid Id { get; set; }
        // Aplicación
        public string NombreSistema { get; set; } = "DC_BOUTIQUE";
        public string VersionInstalada { get; set; } = "1.0.0";
        public string EntornoEjecucion { get; set; } = string.Empty;
        public string EstadoAplicacion { get; set; } = "Operativa";

        // Base de datos (solo lectura)
        public string RutaBaseDatos { get; set; } = string.Empty;
        public long TamanoBaseDatosBytes { get; set; }
        public bool SqliteConectada { get; set; }
        public string JournalMode { get; set; } = "WAL";

        // Impresión
        public string ImpresoraConfigurada { get; set; } = "Predeterminada de Windows";
        public int AnchoPapelMm { get; set; } = 80;
        public string NombreComercialTicket { get; set; } = "DAHIANA CORPORAN BOUTIQUE";
        public string DireccionTicket { get; set; } = string.Empty;
        public string TelefonoTicket { get; set; } = string.Empty;
        public string MensajePieTicket { get; set; } = string.Empty;

        // Regional
        public string Moneda { get; set; } = "DOP";
        public string FormatoFecha { get; set; } = "dd/MM/yyyy";
        public string ZonaHoraria { get; set; } = "America/Santo_Domingo";
        public string CulturaRegional { get; set; } = "es-DO";

        // Resumen respaldos
        public string CarpetaRespaldos { get; set; } = string.Empty;
        public string FrecuenciaRespaldos { get; set; } = string.Empty;
        public string PoliticaRetencionResumen { get; set; } = string.Empty;
    }

    public class RegistroLogTecnicoDto
    {
        public Guid Id { get; set; }
        public DateTime FechaEventoUtc { get; set; }
        public DateTime FechaEventoLocal => FechaEventoUtc.ToLocalTime();
        public TipoLogTecnico Tipo { get; set; }
        public string Severidad { get; set; } = "Error";
        public string Origen { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? DetalleTecnico { get; set; }
        public string? UsuarioRelacionado { get; set; }
    }

    public class AuditoriaTecnicaDto
    {
        public Guid Id { get; set; }
        public DateTime FechaAccionUtc { get; set; }
        public DateTime FechaAccionLocal => FechaAccionUtc.ToLocalTime();
        public string UsuarioTecnico { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public bool Exitosa { get; set; }
        public string? Detalle { get; set; }
    }

    public class TechLogsFilterDto
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public TipoLogTecnico? Tipo { get; set; }
        public string? Severidad { get; set; }
        public string? Busqueda { get; set; }
        public int TotalLogsAlmacenados { get; set; }
        public List<RegistroLogTecnicoDto> Logs { get; set; } = new();
        public List<AuditoriaTecnicaDto> Auditorias { get; set; } = new();
    }

    public class DiagnosticCheckItemDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "Correcto"; // Correcto, Advertencia, Error
        public string Resultado { get; set; } = string.Empty;
        public string DescripcionProblema { get; set; } = "Sin incidencias detectadas.";
        public string Recomendacion { get; set; } = "Ninguna acción requerida.";
    }

    public class EnvironmentInfoDto
    {
        public string MachineName { get; set; } = string.Empty;
        public string OsDescription { get; set; } = string.Empty;
        public string OsArchitecture { get; set; } = string.Empty;
        public string DotNetVersion { get; set; } = string.Empty;
        public string ProcessArchitecture { get; set; } = string.Empty;
        public string BaseDirectory { get; set; } = string.Empty;
        public string ProgramDataDirectory { get; set; } = string.Empty;
        public int ProcessorCount { get; set; }
        public long WorkingSetMb { get; set; }
        public string TimeZoneName { get; set; } = string.Empty;
        public DateTime ServerLocalTime { get; set; }
    }

    public class TechDiagnosticsViewDto
    {
        public string EstadoGeneral { get; set; } = "Correcto";
        public DateTime FechaEjecucionLocal { get; set; } = DateTime.Now;
        public EnvironmentInfoDto Entorno { get; set; } = new();
        public List<DiagnosticCheckItemDto> Comprobaciones { get; set; } = new();
        public int TotalCorrectas => Comprobaciones.Count(c => c.Estado == "Correcto");
        public int TotalAdvertencias => Comprobaciones.Count(c => c.Estado == "Advertencia");
        public int TotalErrores => Comprobaciones.Count(c => c.Estado == "Error");
    }

    public class RegistroMantenimientoDto
    {
        public Guid Id { get; set; }
        public DateTime FechaMantenimientoUtc { get; set; }
        public DateTime FechaMantenimientoLocal => FechaMantenimientoUtc.ToLocalTime();
        public TipoMantenimiento Tipo { get; set; }
        public string DescripcionProblema { get; set; } = string.Empty;
        public string SolucionAplicada { get; set; } = string.Empty;
        public string VersionSistema { get; set; } = "1.0.0";
        public string? VersionAnterior { get; set; }
        public bool RespaldoPrevioRealizado { get; set; }
        public string Resultado { get; set; } = "Exitoso";
        public string? Observaciones { get; set; }
        public string RegistradoPor { get; set; } = string.Empty;
    }

    public class CreateMaintenanceRecordDto
    {
        public TipoMantenimiento Tipo { get; set; } = TipoMantenimiento.MantenimientoPreventivo;
        public string DescripcionProblema { get; set; } = string.Empty;
        public string SolucionAplicada { get; set; } = string.Empty;
        public string? VersionSistema { get; set; }
        public bool RespaldoPrevioRealizado { get; set; }
        public string Resultado { get; set; } = "Exitoso";
        public string? Observaciones { get; set; }
    }

    public class TechMaintenanceHistoryViewDto
    {
        public string VersionInstalada { get; set; } = "1.0.0";
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public TipoMantenimiento? Tipo { get; set; }
        public List<RegistroMantenimientoDto> Registros { get; set; } = new();
    }
}
