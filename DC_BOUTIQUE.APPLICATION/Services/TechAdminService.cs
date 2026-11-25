using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DC_BOUTIQUE.Core.Application.DTOs.TechAdmin;
using DC_BOUTIQUE.Core.Application.DTOs.Ventas;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Helpers;
using DC_BOUTIQUE.Core.Domain.Interfaces;

namespace DC_BOUTIQUE.Core.Application.Services
{
    /// <summary>
    /// Servicio central del Centro del Desarrollador — DC_BOUTIQUE.
    /// 100% local offline sobre SQLite, con bloqueo de concurrencia en respaldos, retención inteligente,
    /// diagnóstico con recomendaciones, configuración técnica e historial de mantenimiento.
    /// </summary>
    public class TechAdminService : ITechAdminService
    {
        private const string SystemProductName = "DC_BOUTIQUE";

        // Garantiza que NUNCA se ejecuten dos respaldos simultáneamente (manuales, prueba o segundo plano)
        private static readonly SemaphoreSlim BackupExecutionLock = new(1, 1);

        private static readonly Regex SensitivePairRegex = new(
            @"(password|passwd|pwd|pin|hash|secret|token|apikey|clave|credential)\s*[:=]\s*[^\s,;""'&]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ITechPersistenceGateway _gateway;
        private readonly IUsuarioService _usuarioService;
        private readonly IAccountService _accountService;
        private readonly ITicketPrintingService _ticketPrintingService;

        public TechAdminService(
            ITechPersistenceGateway gateway,
            IUsuarioService usuarioService,
            IAccountService accountService,
            ITicketPrintingService ticketPrintingService)
        {
            _gateway = gateway;
            _usuarioService = usuarioService;
            _accountService = accountService;
            _ticketPrintingService = ticketPrintingService;
        }

        private static string GetCurrentSystemVersion()
        {
            var asm = Assembly.GetEntryAssembly() ?? typeof(TechAdminService).Assembly;
            var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(infoVer))
            {
                var clean = infoVer.Split('+')[0];
                if (!string.IsNullOrWhiteSpace(clean)) return clean;
            }
            var ver = asm.GetName().Version;
            return ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.0.0";
        }

        // =========================================================================
        // 1. DASHBOARD TÉCNICO
        // =========================================================================
        public async Task<TechDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            var health = await _gateway.GetSqliteHealthAsync(cancellationToken);
            var counts = await _gateway.GetSystemCountsAsync(cancellationToken);
            var backupCfg = await _gateway.GetBackupConfigAsync(cancellationToken);
            var backupDir = ResolveSafeBackupDirectory(backupCfg.CarpetaDestino);
            var backups = ScanBackupsInDirectory(backupDir);
            var lastFile = backups.OrderByDescending(b => b.CreatedAt).FirstOrDefault();

            var since24h = DateTime.UtcNow.AddHours(-24);
            var errors24h = await _gateway.CountLogsAsync(since24h, TipoLogTecnico.Error, cancellationToken)
                + await _gateway.CountLogsAsync(since24h, TipoLogTecnico.Excepcion, cancellationToken)
                + await _gateway.CountLogsAsync(since24h, TipoLogTecnico.Persistencia, cancellationToken)
                + await _gateway.CountLogsAsync(since24h, TipoLogTecnico.Ventas, cancellationToken)
                + await _gateway.CountLogsAsync(since24h, TipoLogTecnico.Respaldos, cancellationToken);
            var authFails24h = await _gateway.CountLogsAsync(since24h, TipoLogTecnico.Autenticacion, cancellationToken);

            var recentLogs = await _gateway.QueryLogsAsync(null, null, null, null, null, 25, cancellationToken);
            var ultimosErrores = recentLogs
                .Where(l => l.Severidad.Equals("Error", StringComparison.OrdinalIgnoreCase)
                    || l.Severidad.Equals("Critical", StringComparison.OrdinalIgnoreCase)
                    || l.Severidad.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                .Take(6)
                .Select(MapLog)
                .ToList();

            var (driveName, freeBytes, totalBytes, usedPct) = GetDiskSpaceMetrics(health.DatabasePath);
            DateTime? lastBackupLocal = backupCfg.UltimoRespaldoExitosoUtc?.ToLocalTime() ?? lastFile?.CreatedAt;
            DateTime? nextScheduledLocal = ComputeNextScheduledBackupLocal(backupCfg);

            var services = new List<ServiceHealthItemDto>
            {
                new()
                {
                    Nombre = "Conexión SQLite Local",
                    Saludable = health.CanConnect,
                    Estado = health.CanConnect ? "Correcto" : "Error",
                    Detalle = health.CanConnect
                        ? $"SQLite v{health.SqliteVersion} · Modo {health.JournalMode} · FK {(health.ForeignKeysEnabled ? "ON" : "OFF")}"
                        : (health.ErrorMessage ?? "Sin conexión")
                },
                new()
                {
                    Nombre = "Esquema y Migraciones",
                    Saludable = health.SchemaUpToDate,
                    Estado = health.SchemaUpToDate ? "Correcto" : "Advertencia",
                    Detalle = health.SchemaUpToDate
                        ? $"{health.TablesFound.Count} tablas sincronizadas correctamente"
                        : $"Faltan: {string.Join(", ", health.MissingExpectedTables)}"
                },
                new()
                {
                    Nombre = "Respaldos Automáticos",
                    Saludable = backupCfg.AutomaticosActivos && string.IsNullOrWhiteSpace(backupCfg.UltimoErrorRespaldo),
                    Estado = !backupCfg.AutomaticosActivos
                        ? "Advertencia"
                        : (string.IsNullOrWhiteSpace(backupCfg.UltimoErrorRespaldo) ? "Correcto" : "Error"),
                    Detalle = !backupCfg.AutomaticosActivos
                        ? "Respaldos automáticos desactivados"
                        : (!string.IsNullOrWhiteSpace(backupCfg.UltimoErrorRespaldo)
                            ? $"Último error: {backupCfg.UltimoErrorRespaldo}"
                            : $"{FormatFrequency(backupCfg.Frecuencia)} · Destino: {backupDir}")
                },
                new()
                {
                    Nombre = "Espacio en Disco",
                    Saludable = freeBytes > 500L * 1024 * 1024,
                    Estado = freeBytes < 200L * 1024 * 1024 ? "Error" : (freeBytes < 1024L * 1024 * 1024 ? "Advertencia" : "Correcto"),
                    Detalle = $"{FormatBytes(freeBytes)} disponibles ({usedPct}% usado en {driveName})"
                },
                new()
                {
                    Nombre = "Almacenamiento de Datos (ProgramData)",
                    Saludable = Directory.Exists(DatabasePathHelper.GetDataDirectory()) && Directory.Exists(backupDir),
                    Estado = Directory.Exists(DatabasePathHelper.GetDataDirectory()) ? "Correcto" : "Error",
                    Detalle = "Datos y respaldos fuera de Program Files (protegidos ante actualizaciones)"
                }
            };

            string estadoGeneral = !health.CanConnect || freeBytes < 200L * 1024 * 1024
                ? "Error"
                : (!health.SchemaUpToDate || backups.Count == 0 || errors24h > 5 || !string.IsNullOrWhiteSpace(backupCfg.UltimoErrorRespaldo)
                    ? "Advertencia"
                    : "Correcto");

            return new TechDashboardDto
            {
                NombreSistema = SystemProductName,
                VersionInstalada = GetCurrentSystemVersion(),
                EstadoAplicacion = "Correcto",
                EstadoGeneral = estadoGeneral,
                SistemaSaludable = estadoGeneral == "Correcto",
                SqliteConectada = health.CanConnect,
                SqliteVersion = health.SqliteVersion,
                JournalMode = health.JournalMode,
                ForeignKeysEnabled = health.ForeignKeysEnabled,
                RutaBaseDatos = health.DatabasePath,
                TamanoBaseDatosBytes = health.FileSizeBytes,
                TamanoWalBytes = health.WalSizeBytes,
                UnidadDisco = driveName,
                EspacioLibreBytes = freeBytes,
                EspacioTotalBytes = totalBytes,
                PorcentajeUsoDisco = usedPct,
                FechaUltimoRespaldoExitoso = lastBackupLocal,
                NombreUltimoRespaldo = backupCfg.UltimoArchivoRespaldo ?? lastFile?.FileName,
                ProximoRespaldoProgramado = nextScheduledLocal,
                RespaldosAutomaticosActivos = backupCfg.AutomaticosActivos,
                FrecuenciaRespaldosTexto = FormatFrequency(backupCfg.Frecuencia),
                TotalRespaldosDisponibles = backups.Count,
                TotalUsuariosRegistrados = counts.TotalUsuarios,
                UsuariosActivos = counts.UsuariosActivos,
                TotalProductos = counts.TotalProductos,
                TotalVariantes = counts.TotalVariantes,
                EsquemaActualizado = health.SchemaUpToDate,
                TotalTablasDetectadas = health.TablesFound.Count,
                ElementosFaltantesEsquema = health.MissingExpectedTables,
                ErroresUltimas24Horas = errors24h,
                FallosAuthUltimas24Horas = authFails24h,
                UltimosErrores = ultimosErrores,
                ServiciosPrincipales = services
            };
        }

        // =========================================================================
        // 2. BASE DE DATOS Y RESPALDOS (MANUALES, AUTOMÁTICOS Y RESTAURACIÓN)
        // =========================================================================
        public async Task<TechDatabaseViewDto> GetDatabaseStatusAsync(CancellationToken cancellationToken = default)
        {
            var health = await _gateway.GetSqliteHealthAsync(cancellationToken);
            var cfg = await _gateway.GetBackupConfigAsync(cancellationToken);
            var backupDir = ResolveSafeBackupDirectory(cfg.CarpetaDestino);
            var backups = ScanBackupsInDirectory(backupDir);
            var backupErrors = await _gateway.QueryLogsAsync(null, null, TipoLogTecnico.Respaldos, null, null, 10, cancellationToken);

            return new TechDatabaseViewDto
            {
                RutaBaseDatos = health.DatabasePath,
                RutaRespaldos = backupDir,
                TamanoBaseDatosBytes = health.FileSizeBytes,
                TamanoWalBytes = health.WalSizeBytes,
                SqliteVersion = health.SqliteVersion,
                JournalMode = health.JournalMode,
                ForeignKeysEnabled = health.ForeignKeysEnabled,
                PageCount = health.PageCount,
                PageSize = health.PageSize,
                FreelistCount = health.FreelistCount,
                EsquemaActualizado = health.SchemaUpToDate,
                TablasDetectadas = health.TablesFound,
                Respaldos = backups,
                ConfiguracionRespaldos = MapBackupConfig(cfg, backupDir),
                ErroresRespaldosRecientes = backupErrors.Select(MapLog).ToList()
            };
        }

        public async Task<TechIntegrityResultDto> VerifyIntegrityAsync(string techUser, CancellationToken cancellationToken = default)
        {
            var report = await _gateway.RunIntegrityCheckAsync(cancellationToken);

            await RecordAuditAsync(
                techUser,
                "BaseDatos",
                "PRAGMA integrity_check",
                report.IsValid,
                report.IsValid ? $"Integridad SQLite OK ({report.DurationMs} ms)" : string.Join("; ", report.Results),
                cancellationToken);

            if (!report.IsValid)
            {
                await LogEventAsync(TipoLogTecnico.Persistencia, "Critical", "TechAdminService.VerifyIntegrityAsync",
                    "La verificación de integridad SQLite reportó inconsistencias.", string.Join(" | ", report.Results), techUser, cancellationToken);
            }

            return new TechIntegrityResultDto
            {
                IsValid = report.IsValid,
                Messages = report.Results,
                CheckedAtLocal = report.CheckedAtUtc.ToLocalTime(),
                DurationMs = report.DurationMs
            };
        }

        public async Task<TechOperationResultDto> CreateSafeBackupAsync(
            string techUser,
            string? note = null,
            string? customDirectory = null,
            bool isTest = false,
            CancellationToken cancellationToken = default)
        {
            if (!await BackupExecutionLock.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken))
            {
                return new TechOperationResultDto
                {
                    Success = false,
                    Message = "Ya existe un respaldo en ejecución. Espere unos segundos e intente nuevamente."
                };
            }

            try
            {
                var cfg = await _gateway.GetBackupConfigAsync(cancellationToken);
                var targetDir = string.IsNullOrWhiteSpace(customDirectory)
                    ? ResolveSafeBackupDirectory(cfg.CarpetaDestino)
                    : customDirectory.Trim();

                if (IsInsideProgramFiles(targetDir))
                {
                    return new TechOperationResultDto
                    {
                        Success = false,
                        Message = "Por seguridad no se permite guardar respaldos dentro de Program Files. Seleccione una ruta fuera de la carpeta de instalación (ej. C:\\ProgramData\\DC_BOUTIQUE\\Backups)."
                    };
                }

                DatabasePathHelper.EnsureDirectoryExists(targetDir);
                if (!Directory.Exists(targetDir))
                {
                    return new TechOperationResultDto
                    {
                        Success = false,
                        Message = $"No se pudo crear ni acceder a la carpeta de destino: {targetDir}"
                    };
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string prefix = isTest
                    ? "PRUEBA"
                    : (string.IsNullOrWhiteSpace(note)
                        ? "MANUAL"
                        : Regex.Replace(note.Trim().ToUpperInvariant(), @"[^A-Z0-9_-]", "_"));
                if (prefix.Length > 20) prefix = prefix[..20];

                var fileName = $"DC_BOUTIQUE_{prefix}_{timestamp}.db";
                var destPath = Path.Combine(targetDir, fileName);

                var (success, message, bytesWritten) = await _gateway.CreateOnlineSqliteBackupAsync(destPath, cancellationToken);

                if (success)
                {
                    cfg.UltimoRespaldoExitosoUtc = DateTime.UtcNow;
                    cfg.UltimoArchivoRespaldo = fileName;
                    cfg.UltimoErrorRespaldo = null;
                    cfg.FechaUltimoErrorUtc = null;
                    cfg.ProximoRespaldoProgramadoUtc = ComputeNextScheduledBackupUtc(cfg);
                    await _gateway.SaveBackupConfigAsync(cfg, cancellationToken);
                }
                else
                {
                    cfg.UltimoErrorRespaldo = message;
                    cfg.FechaUltimoErrorUtc = DateTime.UtcNow;
                    await _gateway.SaveBackupConfigAsync(cfg, cancellationToken);
                    await LogEventAsync(TipoLogTecnico.Respaldos, "Error", "TechAdminService.CreateSafeBackupAsync", message, destPath, techUser, cancellationToken);
                }

                await RecordAuditAsync(
                    techUser,
                    "Respaldos",
                    isTest ? "Respaldo de Prueba" : "Respaldo Manual Consistente",
                    success,
                    success ? $"{fileName} ({FormatBytes(bytesWritten)}) en {targetDir}" : message,
                    cancellationToken);

                return new TechOperationResultDto
                {
                    Success = success,
                    Message = success
                        ? $"Respaldo '{fileName}' creado y verificado con PRAGMA integrity_check ({FormatBytes(bytesWritten)})."
                        : message,
                    SecondaryDetail = success ? destPath : null
                };
            }
            catch (Exception ex)
            {
                await LogEventAsync(TipoLogTecnico.Respaldos, "Error", "TechAdminService.CreateSafeBackupAsync", ex.Message, ex.ToString(), techUser, cancellationToken);
                return new TechOperationResultDto { Success = false, Message = $"Error al generar respaldo: {ex.Message}" };
            }
            finally
            {
                BackupExecutionLock.Release();
            }
        }

        public async Task<TechOperationResultDto> DeleteBackupSafelyAsync(string fileName, string techUser, CancellationToken cancellationToken = default)
        {
            if (!await BackupExecutionLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            {
                return new TechOperationResultDto { Success = false, Message = "No se puede eliminar mientras hay un respaldo en curso." };
            }

            try
            {
                var cfg = await _gateway.GetBackupConfigAsync(cancellationToken);
                var backupDir = ResolveSafeBackupDirectory(cfg.CarpetaDestino);
                var safeName = Path.GetFileName(fileName ?? string.Empty);
                var fullPath = Path.Combine(backupDir, safeName);

                if (!File.Exists(fullPath))
                {
                    return new TechOperationResultDto { Success = false, Message = "El archivo de respaldo especificado no existe." };
                }

                var backups = ScanBackupsInDirectory(backupDir);
                var validBackups = backups.Where(b => b.SizeInBytes > 100).ToList();
                if (validBackups.Count <= 1 && validBackups.Any(b => b.FileName.Equals(safeName, StringComparison.OrdinalIgnoreCase)))
                {
                    return new TechOperationResultDto
                    {
                        Success = false,
                        Message = "Operación bloqueada: no está permitido eliminar el último respaldo válido disponible."
                    };
                }

                if (IsFileLocked(fullPath))
                {
                    return new TechOperationResultDto
                    {
                        Success = false,
                        Message = "No se puede eliminar este respaldo porque está siendo utilizado por otro proceso."
                    };
                }

                File.Delete(fullPath);

                await RecordAuditAsync(techUser, "Respaldos", "Eliminar Respaldo Antiguo", true, safeName, cancellationToken);
                return new TechOperationResultDto { Success = true, Message = $"Respaldo '{safeName}' eliminado correctamente." };
            }
            catch (Exception ex)
            {
                await LogEventAsync(TipoLogTecnico.Respaldos, "Error", "TechAdminService.DeleteBackupSafelyAsync", ex.Message, fileName, techUser, cancellationToken);
                return new TechOperationResultDto { Success = false, Message = $"No se pudo eliminar el respaldo: {ex.Message}" };
            }
            finally
            {
                BackupExecutionLock.Release();
            }
        }

        public async Task<TechOperationResultDto> RestoreBackupSafelyAsync(string fileName, string techUser, CancellationToken cancellationToken = default)
        {
            if (!await BackupExecutionLock.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken))
            {
                return new TechOperationResultDto { Success = false, Message = "Hay otra operación de respaldo en curso. Intente nuevamente en unos segundos." };
            }

            try
            {
                var cfg = await _gateway.GetBackupConfigAsync(cancellationToken);
                var backupDir = ResolveSafeBackupDirectory(cfg.CarpetaDestino);
                var safeName = Path.GetFileName(fileName ?? string.Empty);

                if (string.IsNullOrWhiteSpace(safeName) || !safeName.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                {
                    return new TechOperationResultDto { Success = false, Message = "Nombre de archivo SQLite (.db) no válido." };
                }

                var sourcePath = Path.Combine(backupDir, safeName);
                if (!File.Exists(sourcePath))
                {
                    return new TechOperationResultDto { Success = false, Message = $"El archivo '{safeName}' no existe en {backupDir}." };
                }

                var preRestoreName = $"DC_BOUTIQUE_PRE_RESTORE_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var preRestorePath = Path.Combine(backupDir, preRestoreName);

                var outcome = await _gateway.RestoreSqliteBackupSafelyAsync(sourcePath, preRestorePath, cancellationToken);

                await RecordAuditAsync(techUser, "BaseDatos", $"Restaurar Respaldo ({safeName})", outcome.Success, outcome.Message, cancellationToken);

                // Paso 8 obligatorio: registrar la operación en el Historial de Mantenimiento
                await _gateway.AddMaintenanceRecordAsync(new RegistroMantenimiento
                {
                    Id = Guid.NewGuid(),
                    FechaMantenimientoUtc = DateTime.UtcNow,
                    Tipo = TipoMantenimiento.RestauracionRespaldo,
                    DescripcionProblema = $"Restauración manual solicitada desde el respaldo '{safeName}'.",
                    SolucionAplicada = outcome.Message,
                    VersionSistema = GetCurrentSystemVersion(),
                    RespaldoPrevioRealizado = !string.IsNullOrWhiteSpace(outcome.PreRestoreBackupFileName),
                    Resultado = outcome.Success ? "Exitoso" : "Fallido",
                    Observaciones = outcome.PostRestoreQueryVerified
                        ? "Verificación de integridad y consulta de comprobación post-restauración aprobadas."
                        : "Revisar logs de restauración.",
                    RegistradoPor = techUser,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                }, cancellationToken);

                if (!outcome.Success)
                {
                    await LogEventAsync(TipoLogTecnico.Restauracion, "Critical", "TechAdminService.RestoreBackupSafelyAsync", outcome.Message, safeName, techUser, cancellationToken);
                }

                return new TechOperationResultDto
                {
                    Success = outcome.Success,
                    Message = outcome.Message,
                    SecondaryDetail = outcome.PreRestoreBackupFileName
                };
            }
            finally
            {
                BackupExecutionLock.Release();
            }
        }

        public async Task<TechOperationResultDto> SaveBackupConfigAsync(TechBackupConfigDto dto, string techUser, CancellationToken cancellationToken = default)
        {
            var dest = string.IsNullOrWhiteSpace(dto.CarpetaDestino)
                ? DatabasePathHelper.GetBackupDirectory()
                : dto.CarpetaDestino.Trim();

            if (IsInsideProgramFiles(dest))
            {
                return new TechOperationResultDto
                {
                    Success = false,
                    Message = "La carpeta de respaldos no puede estar dentro de Program Files. Use C:\\ProgramData\\DC_BOUTIQUE\\Backups u otra unidad/carpeta segura."
                };
            }

            if (!TimeSpan.TryParseExact(dto.HoraRespaldoDiario?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out _))
            {
                return new TechOperationResultDto { Success = false, Message = "La hora del respaldo diario debe tener formato HH:mm (ejemplo: 20:00)." };
            }

            try
            {
                DatabasePathHelper.EnsureDirectoryExists(dest);
                var probe = Path.Combine(dest, $".dc_write_test_{Guid.NewGuid():N}.tmp");
                await File.WriteAllTextAsync(probe, "ok", cancellationToken);
                File.Delete(probe);
            }
            catch (Exception ex)
            {
                return new TechOperationResultDto { Success = false, Message = $"La carpeta '{dest}' no tiene permisos de escritura: {ex.Message}" };
            }

            var entity = await _gateway.GetBackupConfigAsync(cancellationToken);
            entity.AutomaticosActivos = dto.AutomaticosActivos && dto.Frecuencia != FrecuenciaRespaldo.Desactivado;
            entity.Frecuencia = dto.AutomaticosActivos ? dto.Frecuencia : FrecuenciaRespaldo.Desactivado;
            entity.RespaldoDiarioActivo = dto.RespaldoDiarioActivo;
            entity.HoraRespaldoDiario = dto.HoraRespaldoDiario!.Trim();
            entity.CarpetaDestino = dest;
            entity.RetenerHorarios = Math.Clamp(dto.RetenerHorarios, 1, 200);
            entity.RetenerDiarios = Math.Clamp(dto.RetenerDiarios, 1, 90);
            entity.RetenerSemanales = Math.Clamp(dto.RetenerSemanales, 1, 52);
            entity.RetenerMensuales = Math.Clamp(dto.RetenerMensuales, 1, 36);
            entity.RetencionDias = Math.Clamp(dto.RetencionDias, 1, 3650);
            entity.ConservarDiarios = dto.ConservarDiarios;
            entity.ConservarSemanales = dto.ConservarSemanales;
            entity.ConservarMensuales = dto.ConservarMensuales;
            entity.ProximoRespaldoProgramadoUtc = ComputeNextScheduledBackupUtc(entity);

            await _gateway.SaveBackupConfigAsync(entity, cancellationToken);
            await RecordAuditAsync(
                techUser,
                "Respaldos",
                "Actualizar Configuración de Respaldos Automáticos",
                true,
                $"Activos: {entity.AutomaticosActivos} | Frecuencia: {FormatFrequency(entity.Frecuencia)} | Diario: {entity.HoraRespaldoDiario} | Destino: {entity.CarpetaDestino}",
                cancellationToken);

            return new TechOperationResultDto { Success = true, Message = "Configuración de respaldos automáticos y retención guardada correctamente." };
        }

        public async Task RunScheduledBackupIfDueAsync(CancellationToken cancellationToken = default)
        {
            if (!await BackupExecutionLock.WaitAsync(0, cancellationToken))
            {
                return; // Otro respaldo ya está corriendo; nunca ejecutar dos simultáneos
            }

            try
            {
                var cfg = await _gateway.GetBackupConfigAsync(cancellationToken);
                if (!cfg.AutomaticosActivos && !cfg.RespaldoDiarioActivo)
                {
                    return;
                }

                var backupDir = ResolveSafeBackupDirectory(cfg.CarpetaDestino);
                if (IsInsideProgramFiles(backupDir))
                {
                    backupDir = DatabasePathHelper.GetBackupDirectory();
                }
                DatabasePathHelper.EnsureDirectoryExists(backupDir);

                var nowUtc = DateTime.UtcNow;
                var nowLocal = DateTime.Now;

                bool needIntervalBackup = cfg.AutomaticosActivos
                    && cfg.Frecuencia != FrecuenciaRespaldo.Desactivado
                    && (cfg.UltimoRespaldoExitosoUtc == null
                        || (nowUtc - cfg.UltimoRespaldoExitosoUtc.Value).TotalMinutes >= (int)cfg.Frecuencia);

                bool needDailyBackup = false;
                if (cfg.RespaldoDiarioActivo
                    && TimeSpan.TryParseExact(cfg.HoraRespaldoDiario, @"hh\:mm", CultureInfo.InvariantCulture, out var dailyTime))
                {
                    bool passedDailyHour = nowLocal.TimeOfDay >= dailyTime;
                    bool ranToday = cfg.UltimoRespaldoDiarioUtc.HasValue
                        && cfg.UltimoRespaldoDiarioUtc.Value.ToLocalTime().Date == nowLocal.Date;
                    needDailyBackup = passedDailyHour && !ranToday;
                }

                if (!needIntervalBackup && !needDailyBackup)
                {
                    var expectedNext = ComputeNextScheduledBackupUtc(cfg);
                    if (cfg.ProximoRespaldoProgramadoUtc != expectedNext)
                    {
                        cfg.ProximoRespaldoProgramadoUtc = expectedNext;
                        await _gateway.SaveBackupConfigAsync(cfg, cancellationToken);
                    }
                    return;
                }

                var tag = needDailyBackup ? "AUTO_DIARIO" : "AUTO";
                var fileName = $"DC_BOUTIQUE_{tag}_{nowLocal:yyyyMMdd_HHmmss}.db";
                var destPath = Path.Combine(backupDir, fileName);

                var (success, message, bytesWritten) = await _gateway.CreateOnlineSqliteBackupAsync(destPath, cancellationToken);
                if (success)
                {
                    cfg.UltimoRespaldoExitosoUtc = DateTime.UtcNow;
                    cfg.UltimoArchivoRespaldo = fileName;
                    if (needDailyBackup) cfg.UltimoRespaldoDiarioUtc = DateTime.UtcNow;
                    cfg.UltimoErrorRespaldo = null;
                    cfg.FechaUltimoErrorUtc = null;
                    cfg.ProximoRespaldoProgramadoUtc = ComputeNextScheduledBackupUtc(cfg);
                    await _gateway.SaveBackupConfigAsync(cfg, cancellationToken);

                    ApplyRetentionPolicy(backupDir, cfg);

                    await RecordAuditAsync("ServicioAutomático", "Respaldos", $"Respaldo Automático ({tag})", true, $"{fileName} ({FormatBytes(bytesWritten)})", cancellationToken);
                }
                else
                {
                    cfg.UltimoErrorRespaldo = message;
                    cfg.FechaUltimoErrorUtc = DateTime.UtcNow;
                    await _gateway.SaveBackupConfigAsync(cfg, cancellationToken);
                    await LogEventAsync(TipoLogTecnico.Respaldos, "Error", "AutomatedBackupBackgroundService", message, destPath, "Sistema", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await LogEventAsync(TipoLogTecnico.Respaldos, "Error", "AutomatedBackupBackgroundService", ex.Message, ex.ToString(), "Sistema", cancellationToken);
            }
            finally
            {
                BackupExecutionLock.Release();
            }
        }

        public async Task<string> GetConfiguredBackupDirectoryAsync(CancellationToken cancellationToken = default)
        {
            var cfg = await _gateway.GetBackupConfigAsync(cancellationToken);
            return ResolveSafeBackupDirectory(cfg.CarpetaDestino);
        }

        public async Task<(byte[] Content, string FileName)> ExportTechnicalDbReportJsonAsync(string techUser, CancellationToken cancellationToken = default)
        {
            var dbStatus = await GetDatabaseStatusAsync(cancellationToken);
            var integrity = await _gateway.RunIntegrityCheckAsync(cancellationToken);
            var env = BuildEnvironmentInfo();

            var payload = new
            {
                GeneratedAtUtc = DateTime.UtcNow,
                System = SystemProductName,
                Version = GetCurrentSystemVersion(),
                Environment = env,
                Database = new
                {
                    dbStatus.RutaBaseDatos,
                    dbStatus.RutaRespaldos,
                    dbStatus.TamanoBaseDatosBytes,
                    dbStatus.TamanoWalBytes,
                    dbStatus.SqliteVersion,
                    dbStatus.JournalMode,
                    dbStatus.ForeignKeysEnabled,
                    dbStatus.PageCount,
                    dbStatus.PageSize,
                    dbStatus.FreelistCount,
                    dbStatus.EsquemaActualizado,
                    dbStatus.TablasDetectadas,
                    IntegrityCheck = new { integrity.IsValid, integrity.DurationMs, integrity.Results },
                    BackupsCount = dbStatus.Respaldos.Count,
                    Backups = dbStatus.Respaldos.Select(b => new { b.FileName, b.SizeInBytes, b.CreatedAt, b.TipoOrigen })
                }
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            var fileName = $"DC_BOUTIQUE_TechDB_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            await RecordAuditAsync(techUser, "BaseDatos", "Exportar Informe Técnico JSON", true, fileName, cancellationToken);

            return (Encoding.UTF8.GetBytes(json), fileName);
        }

        // =========================================================================
        // 3. USUARIOS Y ACCESOS TÉCNICOS
        // =========================================================================
        public async Task<TechUsersViewDto> GetUsersAndAccessAsync(CancellationToken cancellationToken = default)
        {
            var usuarios = await _usuarioService.GetAllAsync(cancellationToken);
            var emails = await _accountService.GetUserEmailsByUserNameAsync();
            var failedAuth = await _gateway.QueryLogsAsync(null, null, TipoLogTecnico.Autenticacion, null, null, 40, cancellationToken);
            var audits = await _gateway.GetRecentAuditsAsync(60, cancellationToken);

            var rows = new List<TechUserRowDto>();
            foreach (var u in usuarios.OrderByDescending(x => x.Rol == RolUsuario.Desarrollador).ThenByDescending(x => x.EsPrincipal).ThenBy(x => x.NombreCompleto))
            {
                emails.TryGetValue(u.NombreUsuario.ToLowerInvariant(), out var email);
                bool hasOps = await _gateway.UserHasCommercialOperationsAsync(u.Id, cancellationToken);

                rows.Add(new TechUserRowDto
                {
                    Id = u.Id,
                    NombreCompleto = u.NombreCompleto,
                    NombreUsuario = u.NombreUsuario,
                    Correo = string.IsNullOrWhiteSpace(email) ? "Sin correo asignado" : email,
                    Rol = u.Rol,
                    EsPrincipal = u.EsPrincipal,
                    Activo = u.Activo,
                    FechaCreacionLocal = u.FechaCreacion.ToLocalTime(),
                    UltimoAccesoLocal = u.UltimoAcceso?.ToLocalTime(),
                    TieneOperacionesComerciales = hasOps
                });
            }

            return new TechUsersViewDto
            {
                Usuarios = rows,
                IntentosFallidos = failedAuth.Select(MapLog).ToList(),
                EventosAcceso = audits
                    .Where(a => a.Modulo.Equals("Seguridad", StringComparison.OrdinalIgnoreCase)
                        || a.Modulo.Equals("Usuarios", StringComparison.OrdinalIgnoreCase))
                    .Take(40)
                    .Select(MapAudit)
                    .ToList()
            };
        }

        public async Task<TechOperationResultDto> ToggleUserActiveTechAsync(Guid targetUserId, bool activar, string techUser, CancellationToken cancellationToken = default)
        {
            var user = await _usuarioService.GetByIdAsync(targetUserId, cancellationToken);
            if (user == null)
            {
                return new TechOperationResultDto { Success = false, Message = "Usuario no encontrado." };
            }

            if (!activar && (user.EsPrincipal || user.Rol == RolUsuario.Desarrollador))
            {
                return new TechOperationResultDto
                {
                    Success = false,
                    Message = "No está permitido desactivar al Administrador Principal ni a la cuenta Desarrollador."
                };
            }

            var res = activar
                ? await _usuarioService.ActivarAsync(targetUserId, cancellationToken)
                : await _usuarioService.DeleteAsync(targetUserId, cancellationToken);

            if (!res.Succeeded)
            {
                return new TechOperationResultDto { Success = false, Message = res.Message ?? "No se pudo actualizar el estado del usuario." };
            }

            await _accountService.SetActiveStateByTechAdminAsync(user.NombreUsuario, activar);
            await RecordAuditAsync(
                techUser,
                "Usuarios",
                activar ? "Activar Cuenta" : "Desactivar Cuenta",
                true,
                $"Usuario: {user.NombreUsuario} ({user.Rol})",
                cancellationToken);

            return new TechOperationResultDto
            {
                Success = true,
                Message = $"Cuenta '{user.NombreUsuario}' {(activar ? "activada" : "desactivada")} correctamente."
            };
        }

        public async Task<TechOperationResultDto> ResetUserPasswordTechAsync(Guid targetUserId, string newPassword, string techUser, CancellationToken cancellationToken = default)
        {
            var user = await _usuarioService.GetByIdAsync(targetUserId, cancellationToken);
            if (user == null)
            {
                return new TechOperationResultDto { Success = false, Message = "Usuario no encontrado." };
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            {
                return new TechOperationResultDto { Success = false, Message = "La nueva contraseña debe tener al menos 4 caracteres." };
            }

            if (user.Rol == RolUsuario.Desarrollador && newPassword.Length < 8)
            {
                return new TechOperationResultDto { Success = false, Message = "La contraseña del rol Desarrollador debe tener mínimo 8 caracteres." };
            }

            var domainRes = await _usuarioService.ResetPasswordByTechAsync(targetUserId, newPassword, cancellationToken);
            if (!domainRes.Succeeded)
            {
                return new TechOperationResultDto { Success = false, Message = domainRes.Message ?? "No se pudo restablecer la contraseña." };
            }

            var identityRes = await _accountService.ResetPasswordByTechAdminAsync(user.NombreUsuario, newPassword);

            // Nunca registrar la contraseña en auditoría ni logs
            await RecordAuditAsync(
                techUser,
                "Usuarios",
                "Restablecer Contraseña (Seguro)",
                identityRes.Success,
                $"Usuario: {user.NombreUsuario} ({user.Rol}) — credenciales actualizadas sin registro en texto plano.",
                cancellationToken);

            return new TechOperationResultDto
            {
                Success = true,
                Message = $"Contraseña de '{user.NombreUsuario}' restablecida de forma segura en Identity y Dominio."
            };
        }

        public async Task RegisterUserLastAccessAsync(Guid usuarioId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _gateway.UpdateUserLastAccessAsync(usuarioId, DateTime.UtcNow, cancellationToken);
            }
            catch { }
        }

        // =========================================================================
        // 4. CONFIGURACIÓN TÉCNICA + IMPRESIÓN DE PRUEBA
        // =========================================================================
        public async Task<TechConfigViewDto> GetTechnicalConfigAsync(CancellationToken cancellationToken = default)
        {
            var techCfg = await _gateway.GetTechConfigAsync(cancellationToken);
            var health = await _gateway.GetSqliteHealthAsync(cancellationToken);
            var bkpCfg = await _gateway.GetBackupConfigAsync(cancellationToken);

            return new TechConfigViewDto
            {
                Id = techCfg.Id,
                NombreSistema = SystemProductName,
                VersionInstalada = GetCurrentSystemVersion(),
                EntornoEjecucion = $"{RuntimeInformation.OSDescription} · {RuntimeInformation.FrameworkDescription}",
                EstadoAplicacion = health.CanConnect ? "Operativa" : "Degradada",
                RutaBaseDatos = health.DatabasePath,
                TamanoBaseDatosBytes = health.FileSizeBytes,
                SqliteConectada = health.CanConnect,
                JournalMode = health.JournalMode,
                ImpresoraConfigurada = techCfg.ImpresoraConfigurada,
                AnchoPapelMm = techCfg.AnchoPapelMm,
                NombreComercialTicket = techCfg.NombreComercialTicket,
                DireccionTicket = techCfg.DireccionTicket,
                TelefonoTicket = techCfg.TelefonoTicket,
                MensajePieTicket = techCfg.MensajePieTicket,
                Moneda = techCfg.Moneda,
                FormatoFecha = techCfg.FormatoFecha,
                ZonaHoraria = techCfg.ZonaHoraria,
                CulturaRegional = techCfg.CulturaRegional,
                CarpetaRespaldos = ResolveSafeBackupDirectory(bkpCfg.CarpetaDestino),
                FrecuenciaRespaldos = bkpCfg.AutomaticosActivos ? FormatFrequency(bkpCfg.Frecuencia) : "Desactivados",
                PoliticaRetencionResumen = $"{bkpCfg.RetenerHorarios} horarios · {bkpCfg.RetenerDiarios} diarios · {bkpCfg.RetenerSemanales} semanales · {bkpCfg.RetenerMensuales} mensuales ({bkpCfg.RetencionDias} días)"
            };
        }

        public async Task<TechOperationResultDto> SaveTechnicalConfigAsync(TechConfigViewDto dto, string techUser, CancellationToken cancellationToken = default)
        {
            if (dto.AnchoPapelMm != 80 && dto.AnchoPapelMm != 58)
            {
                return new TechOperationResultDto { Success = false, Message = "El ancho de papel permitido para tickets térmicos es 80 mm o 58 mm." };
            }

            if (string.IsNullOrWhiteSpace(dto.ImpresoraConfigurada) || string.IsNullOrWhiteSpace(dto.NombreComercialTicket))
            {
                return new TechOperationResultDto { Success = false, Message = "Debe especificar el nombre de la impresora y el nombre comercial del ticket." };
            }

            var allowedCurrencies = new[] { "DOP", "USD", "EUR" };
            var moneda = (dto.Moneda ?? "DOP").Trim().ToUpperInvariant();
            if (!allowedCurrencies.Contains(moneda))
            {
                return new TechOperationResultDto { Success = false, Message = "Moneda no válida. Seleccione DOP, USD o EUR." };
            }

            var entity = await _gateway.GetTechConfigAsync(cancellationToken);
            entity.ImpresoraConfigurada = dto.ImpresoraConfigurada.Trim();
            entity.AnchoPapelMm = dto.AnchoPapelMm;
            entity.NombreComercialTicket = dto.NombreComercialTicket.Trim();
            entity.DireccionTicket = (dto.DireccionTicket ?? string.Empty).Trim();
            entity.TelefonoTicket = (dto.TelefonoTicket ?? string.Empty).Trim();
            entity.MensajePieTicket = (dto.MensajePieTicket ?? string.Empty).Trim();
            entity.Moneda = moneda;
            entity.FormatoFecha = string.IsNullOrWhiteSpace(dto.FormatoFecha) ? "dd/MM/yyyy" : dto.FormatoFecha.Trim();
            entity.ZonaHoraria = string.IsNullOrWhiteSpace(dto.ZonaHoraria) ? "America/Santo_Domingo" : dto.ZonaHoraria.Trim();
            entity.CulturaRegional = string.IsNullOrWhiteSpace(dto.CulturaRegional) ? "es-DO" : dto.CulturaRegional.Trim();
            entity.VersionRegistrada = GetCurrentSystemVersion();

            await _gateway.SaveTechConfigAsync(entity, cancellationToken);
            await RecordAuditAsync(
                techUser,
                "Configuración",
                "Guardar Configuración Técnica",
                true,
                $"Impresora: {entity.ImpresoraConfigurada} ({entity.AnchoPapelMm}mm) | Moneda: {entity.Moneda} | Cultura: {entity.CulturaRegional}",
                cancellationToken);

            return new TechOperationResultDto { Success = true, Message = "Configuración técnica validada y guardada correctamente." };
        }

        public async Task<string> GenerateTestTicketHtmlAsync(string techUser, CancellationToken cancellationToken = default)
        {
            var cfg = await _gateway.GetTechConfigAsync(cancellationToken);
            var info = new BoutiqueInfoModel
            {
                NombreComercial = cfg.NombreComercialTicket,
                Direccion = cfg.DireccionTicket,
                Telefono = cfg.TelefonoTicket,
                MensajePie = cfg.MensajePieTicket,
                AnchoPapelMm = cfg.AnchoPapelMm
            };

            var sampleVenta = new VentaDto
            {
                Id = Guid.NewGuid(),
                NumeroFactura = "TEST-PRINT-0001",
                FechaVenta = DateTime.Now,
                NombreCliente = "PRUEBA DE IMPRESORA",
                NombreUsuario = techUser,
                Subtotal = 100.00m,
                DescuentoTotal = 0m,
                ImpuestoTotal = 0m,
                TotalNeto = 100.00m,
                Detalles = new List<DetalleVentaDto>
                {
                    new()
                    {
                        SkuSnapshot = "TEST-80MM",
                        NombreProductoSnapshot = $"Prueba Térmica {cfg.AnchoPapelMm}mm ({cfg.ImpresoraConfigurada})",
                        TallaSnapshot = "N/A",
                        ColorSnapshot = "OK",
                        Cantidad = 1,
                        PrecioUnitario = 100.00m,
                        SubtotalLinea = 100.00m
                    }
                },
                Pagos = new List<PagoVentaDto>
                {
                    new() { MetodoPago = MetodoPago.Efectivo, Monto = 100.00m }
                }
            };

            await RecordAuditAsync(techUser, "Configuración", "Impresión de Prueba de Ticket", true, $"Ancho: {cfg.AnchoPapelMm}mm | Impresora: {cfg.ImpresoraConfigurada}", cancellationToken);
            return _ticketPrintingService.GenerateTicketHtml(sampleVenta, info);
        }

        // =========================================================================
        // 5. LOGS Y ERRORES DEL SISTEMA
        // =========================================================================
        public async Task LogEventAsync(
            TipoLogTecnico tipo,
            string severidad,
            string origen,
            string mensaje,
            string? detalleTecnico = null,
            string? usuarioRelacionado = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _gateway.AddLogAsync(new RegistroLogTecnico
                {
                    Id = Guid.NewGuid(),
                    FechaEvento = DateTime.UtcNow,
                    Tipo = tipo,
                    Severidad = string.IsNullOrWhiteSpace(severidad) ? "Error" : severidad.Trim(),
                    Origen = SanitizeSensitiveText(origen, 160),
                    Mensaje = SanitizeSensitiveText(mensaje, 1000),
                    DetalleTecnico = string.IsNullOrWhiteSpace(detalleTecnico) ? null : SanitizeSensitiveText(detalleTecnico, 4000),
                    UsuarioRelacionado = string.IsNullOrWhiteSpace(usuarioRelacionado) ? null : SanitizeSensitiveText(usuarioRelacionado, 80),
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                }, cancellationToken);
            }
            catch { }
        }

        public async Task RecordAuditAsync(
            string usuarioTecnico,
            string modulo,
            string accion,
            bool exitosa,
            string? detalle = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _gateway.AddAuditAsync(new AuditoriaTecnica
                {
                    Id = Guid.NewGuid(),
                    FechaAccion = DateTime.UtcNow,
                    UsuarioTecnico = string.IsNullOrWhiteSpace(usuarioTecnico) ? "Sistema" : usuarioTecnico.Trim(),
                    Modulo = string.IsNullOrWhiteSpace(modulo) ? "General" : modulo.Trim(),
                    Accion = SanitizeSensitiveText(accion, 180),
                    Exitosa = exitosa,
                    Detalle = string.IsNullOrWhiteSpace(detalle) ? null : SanitizeSensitiveText(detalle, 1500),
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                }, cancellationToken);
            }
            catch { }
        }

        public async Task<TechLogsFilterDto> QueryLogsAndAuditsAsync(
            DateTime? desdeLocal,
            DateTime? hastaLocal,
            TipoLogTecnico? tipo,
            string? severidad,
            string? busqueda = null,
            CancellationToken cancellationToken = default)
        {
            DateTime? desdeUtc = desdeLocal?.Date.ToUniversalTime();
            DateTime? hastaUtc = hastaLocal?.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

            var logs = await _gateway.QueryLogsAsync(desdeUtc, hastaUtc, tipo, severidad, busqueda, 300, cancellationToken);
            var audits = await _gateway.GetRecentAuditsAsync(100, cancellationToken);
            var total = await _gateway.CountLogsAsync(null, null, cancellationToken);

            return new TechLogsFilterDto
            {
                Desde = desdeLocal,
                Hasta = hastaLocal,
                Tipo = tipo,
                Severidad = severidad,
                Busqueda = busqueda,
                TotalLogsAlmacenados = total,
                Logs = logs.Select(MapLog).ToList(),
                Auditorias = audits.Select(MapAudit).ToList()
            };
        }

        public async Task<TechOperationResultDto> PurgeOldLogsAsync(int olderThanDays, string techUser, CancellationToken cancellationToken = default)
        {
            int safeDays = Math.Clamp(olderThanDays, 7, 3650);
            var deleted = await _gateway.PurgeOldLogsAsync(DateTime.UtcNow.AddDays(-safeDays), cancellationToken);

            await RecordAuditAsync(techUser, "Logs", $"Limpiar Logs Antiguos (> {safeDays} días)", true, $"Eliminados: {deleted}", cancellationToken);

            return new TechOperationResultDto
            {
                Success = true,
                Message = deleted > 0
                    ? $"Limpieza completada: se eliminaron {deleted} registro(s) con más de {safeDays} días."
                    : $"No se encontraron registros con más de {safeDays} días para limpiar."
            };
        }

        public async Task<(byte[] Content, string ContentType, string FileName)> ExportLogsAsync(
            string format,
            DateTime? desdeLocal,
            DateTime? hastaLocal,
            TipoLogTecnico? tipo,
            string? severidad,
            string? busqueda,
            string techUser,
            CancellationToken cancellationToken = default)
        {
            DateTime? desdeUtc = desdeLocal?.Date.ToUniversalTime();
            DateTime? hastaUtc = hastaLocal?.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            var logs = await _gateway.QueryLogsAsync(desdeUtc, hastaUtc, tipo, severidad, busqueda, 2000, cancellationToken);

            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                sb.AppendLine("FechaLocal,Categoria,Severidad,Origen,UsuarioRelacionado,Mensaje,DetalleTecnico");
                foreach (var l in logs)
                {
                    sb.AppendLine(string.Join(",",
                        CsvEscape(l.FechaEvento.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")),
                        CsvEscape(l.Tipo.ToString()),
                        CsvEscape(l.Severidad),
                        CsvEscape(l.Origen),
                        CsvEscape(l.UsuarioRelacionado ?? string.Empty),
                        CsvEscape(l.Mensaje),
                        CsvEscape(l.DetalleTecnico ?? string.Empty)));
                }

                var csvFile = $"DC_BOUTIQUE_Logs_{stamp}.csv";
                await RecordAuditAsync(techUser, "Logs", "Exportar Logs (CSV)", true, $"{logs.Count} registro(s)", cancellationToken);
                return (Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", csvFile);
            }
            else
            {
                var json = JsonSerializer.Serialize(new { ExportedAtUtc = DateTime.UtcNow, Count = logs.Count, Entries = logs.Select(MapLog) },
                    new JsonSerializerOptions { WriteIndented = true });
                var jsonFile = $"DC_BOUTIQUE_Logs_{stamp}.json";
                await RecordAuditAsync(techUser, "Logs", "Exportar Logs (JSON)", true, $"{logs.Count} registro(s)", cancellationToken);
                return (Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8", jsonFile);
            }
        }

        // =========================================================================
        // 6. DIAGNÓSTICO DEL SISTEMA (9 COMPROBACIONES CON RECOMENDACIONES)
        // =========================================================================
        public async Task<TechDiagnosticsViewDto> RunFullDiagnosticsAsync(string? techUser = null, CancellationToken cancellationToken = default)
        {
            var health = await _gateway.GetSqliteHealthAsync(cancellationToken);
            var integrity = await _gateway.RunIntegrityCheckAsync(cancellationToken);
            var bkpCfg = await _gateway.GetBackupConfigAsync(cancellationToken);
            var techCfg = await _gateway.GetTechConfigAsync(cancellationToken);
            var backupDir = ResolveSafeBackupDirectory(bkpCfg.CarpetaDestino);
            var backups = ScanBackupsInDirectory(backupDir);
            var (driveName, freeBytes, totalBytes, usedPct) = GetDiskSpaceMetrics(health.DatabasePath);

            var checks = new List<DiagnosticCheckItemDto>
            {
                // 1. Conexión a SQLite
                new()
                {
                    Nombre = "1. Conexión a SQLite",
                    Estado = health.CanConnect ? "Correcto" : "Error",
                    Resultado = health.CanConnect ? $"Conectada (SQLite v{health.SqliteVersion}, WAL: {health.JournalMode})" : "Sin conexión",
                    DescripcionProblema = health.CanConnect ? "Sin problemas detectados." : (health.ErrorMessage ?? "No se pudo abrir la base de datos SQLite."),
                    Recomendacion = health.CanConnect ? "Ninguna acción requerida." : "Verifique que el archivo C:\\ProgramData\\DC_BOUTIQUE\\Data\\DC_BOUTIQUE.db exista y no esté bloqueado."
                },
                // 2. Integridad de la base de datos
                new()
                {
                    Nombre = "2. Integridad de la Base de Datos",
                    Estado = integrity.IsValid ? "Correcto" : "Error",
                    Resultado = integrity.IsValid ? $"PRAGMA integrity_check = ok ({integrity.DurationMs} ms)" : "Falló PRAGMA integrity_check",
                    DescripcionProblema = integrity.IsValid ? "Estructura interna y páginas SQLite íntegras." : string.Join("; ", integrity.Results),
                    Recomendacion = integrity.IsValid ? "Ninguna acción requerida." : "Cree un respaldo preventivo de emergencia y restaure el último respaldo íntegro desde Base de Datos y Respaldos."
                },
                // 3. Existencia de directorios necesarios
                BuildDirectoriesCheck(backupDir),
                // 4. Permisos de escritura
                BuildWritePermissionsCheck(backupDir),
                // 5. Disponibilidad del directorio de respaldos
                new()
                {
                    Nombre = "5. Disponibilidad del Directorio de Respaldos",
                    Estado = !Directory.Exists(backupDir) || IsInsideProgramFiles(backupDir)
                        ? "Error"
                        : (backups.Count == 0 ? "Advertencia" : "Correcto"),
                    Resultado = Directory.Exists(backupDir) ? $"{backupDir} ({backups.Count} respaldo(s))" : "Directorio inaccesible",
                    DescripcionProblema = !Directory.Exists(backupDir)
                        ? "La carpeta de respaldos configurada no existe o no está accesible."
                        : (IsInsideProgramFiles(backupDir)
                            ? "La carpeta de respaldos está dentro de Program Files (riesgo en actualizaciones)."
                            : (backups.Count == 0 ? "La carpeta existe pero aún no contiene copias de seguridad." : "Carpeta disponible fuera de Program Files.")),
                    Recomendacion = backups.Count == 0
                        ? "Ejecute un respaldo manual o de prueba desde Base de Datos y Respaldos."
                        : (IsInsideProgramFiles(backupDir) ? "Cambie la carpeta de destino a C:\\ProgramData\\DC_BOUTIQUE\\Backups." : "Ninguna acción requerida.")
                },
                // 6. Estado de las migraciones / esquema
                new()
                {
                    Nombre = "6. Estado de las Migraciones y Esquema",
                    Estado = health.SchemaUpToDate ? "Correcto" : "Advertencia",
                    Resultado = health.SchemaUpToDate ? $"{health.TablesFound.Count} tablas requeridas presentes" : $"Faltan {health.MissingExpectedTables.Count} elemento(s)",
                    DescripcionProblema = health.SchemaUpToDate ? "Esquema completo y compatible con la versión actual." : $"Elementos faltantes: {string.Join(", ", health.MissingExpectedTables)}",
                    Recomendacion = health.SchemaUpToDate ? "Ninguna acción requerida." : "Reinicie la aplicación para aplicar la evolución segura de esquema SQLite sin perder datos."
                },
                // 7. Configuración de impresión
                new()
                {
                    Nombre = "7. Configuración de Impresión (Tickets)",
                    Estado = (techCfg.AnchoPapelMm == 80 || techCfg.AnchoPapelMm == 58) && !string.IsNullOrWhiteSpace(techCfg.ImpresoraConfigurada)
                        ? "Correcto"
                        : "Advertencia",
                    Resultado = $"{techCfg.ImpresoraConfigurada} · Papel {techCfg.AnchoPapelMm} mm",
                    DescripcionProblema = string.IsNullOrWhiteSpace(techCfg.ImpresoraConfigurada)
                        ? "No hay impresora térmica configurada."
                        : "Parámetros de comprobante térmico configurados.",
                    Recomendacion = "Puede comprobar la salida física con el botón 'Impresión de Prueba' en Configuración Técnica."
                },
                // 8. Espacio disponible en disco
                new()
                {
                    Nombre = "8. Espacio Disponible en Disco",
                    Estado = freeBytes < 200L * 1024 * 1024 ? "Error" : (freeBytes < 1024L * 1024 * 1024 ? "Advertencia" : "Correcto"),
                    Resultado = $"{FormatBytes(freeBytes)} libres de {FormatBytes(totalBytes)} ({usedPct}% usado en {driveName})",
                    DescripcionProblema = freeBytes < 200L * 1024 * 1024
                        ? "Espacio crítico en disco: podrían fallar escrituras SQLite o nuevos respaldos."
                        : (freeBytes < 1024L * 1024 * 1024 ? "Espacio en disco bajo (< 1 GB libre)." : "Espacio suficiente para operación y respaldos."),
                    Recomendacion = freeBytes < 1024L * 1024 * 1024
                        ? "Libere espacio en la unidad del sistema o ajuste la política de retención de respaldos."
                        : "Ninguna acción requerida."
                }
            };

            // 9. Estado general de la aplicación
            bool hasErr = checks.Any(c => c.Estado == "Error");
            bool hasWarn = checks.Any(c => c.Estado == "Advertencia");
            string overall = hasErr ? "Error" : (hasWarn ? "Advertencia" : "Correcto");

            checks.Add(new DiagnosticCheckItemDto
            {
                Nombre = "9. Estado General de la Aplicación",
                Estado = overall,
                Resultado = $"DC_BOUTIQUE v{GetCurrentSystemVersion()} · {checks.Count(c => c.Estado == "Correcto")}/8 comprobaciones en estado Correcto",
                DescripcionProblema = hasErr
                    ? "Existen comprobaciones críticas en estado Error que requieren atención técnica."
                    : (hasWarn ? "Existen advertencias preventivas recomendadas para revisar." : "Todos los subsistemas locales operan con normalidad."),
                Recomendacion = hasErr
                    ? "Revise las comprobaciones marcadas en rojo y los registros en Logs y Errores."
                    : (hasWarn ? "Atienda las advertencias amarillas para mantener la instalación óptima." : "Mantenga activos los respaldos automáticos.")
            });

            if (!string.IsNullOrWhiteSpace(techUser))
            {
                await RecordAuditAsync(techUser, "Diagnóstico", "Ejecutar Diagnóstico Completo (9 comprobaciones)", !hasErr, $"Estado general: {overall}", cancellationToken);
            }

            return new TechDiagnosticsViewDto
            {
                EstadoGeneral = overall,
                FechaEjecucionLocal = DateTime.Now,
                Entorno = BuildEnvironmentInfo(),
                Comprobaciones = checks
            };
        }

        public async Task<(byte[] ZipBytes, string FileName)> GenerateDiagnosticPackageZipAsync(string techUser, CancellationToken cancellationToken = default)
        {
            var diag = await RunFullDiagnosticsAsync(null, cancellationToken);
            var dashboard = await GetDashboardAsync(cancellationToken);
            var techCfg = await GetTechnicalConfigAsync(cancellationToken);
            var logs = await _gateway.QueryLogsAsync(DateTime.UtcNow.AddDays(-30), null, null, null, null, 500, cancellationToken);
            var audits = await _gateway.GetRecentAuditsAsync(150, cancellationToken);
            var maint = await _gateway.QueryMaintenanceRecordsAsync(null, null, null, cancellationToken);

            var jsonOpts = new JsonSerializerOptions { WriteIndented = true };

            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                AddJsonEntry(zip, "01_resumen_dashboard.json", new
                {
                    GeneratedAtUtc = DateTime.UtcNow,
                    PrivacyNotice = "Paquete exclusivamente técnico. No incluye la base de datos .db ni credenciales.",
                    dashboard.NombreSistema,
                    dashboard.VersionInstalada,
                    dashboard.EstadoGeneral,
                    dashboard.SqliteConectada,
                    dashboard.SqliteVersion,
                    dashboard.JournalMode,
                    dashboard.TamanoBaseDatosBytes,
                    dashboard.EspacioLibreBytes,
                    dashboard.TotalRespaldosDisponibles,
                    dashboard.TotalUsuariosRegistrados,
                    dashboard.TotalProductos,
                    dashboard.TotalVariantes
                }, jsonOpts);
                AddJsonEntry(zip, "02_diagnostico_9_comprobaciones.json", diag, jsonOpts);
                AddJsonEntry(zip, "03_configuracion_tecnica.json", techCfg, jsonOpts);
                AddJsonEntry(zip, "04_logs_sanitizados_30d.json", logs.Select(MapLog), jsonOpts);
                AddJsonEntry(zip, "05_auditoria_tecnica.json", audits.Select(MapAudit), jsonOpts);
                AddJsonEntry(zip, "06_historial_mantenimiento.json", maint.Select(MapMaintenance), jsonOpts);
            }

            var fileName = $"DC_BOUTIQUE_Diagnostico_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            await RecordAuditAsync(techUser, "Diagnóstico", "Exportar Paquete de Diagnóstico ZIP", true, fileName, cancellationToken);

            return (ms.ToArray(), fileName);
        }

        // =========================================================================
        // 7. HISTORIAL DE MANTENIMIENTO + PROCEDIMIENTO ACTUALIZACIÓN INNO SETUP 6
        // =========================================================================
        public async Task<TechMaintenanceHistoryViewDto> GetMaintenanceHistoryAsync(
            DateTime? desdeLocal,
            DateTime? hastaLocal,
            TipoMantenimiento? tipo,
            CancellationToken cancellationToken = default)
        {
            DateTime? desdeUtc = desdeLocal?.Date.ToUniversalTime();
            DateTime? hastaUtc = hastaLocal?.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            var records = await _gateway.QueryMaintenanceRecordsAsync(desdeUtc, hastaUtc, tipo, cancellationToken);

            return new TechMaintenanceHistoryViewDto
            {
                VersionInstalada = GetCurrentSystemVersion(),
                Desde = desdeLocal,
                Hasta = hastaLocal,
                Tipo = tipo,
                Registros = records.Select(MapMaintenance).ToList()
            };
        }

        public async Task<TechOperationResultDto> AddMaintenanceRecordAsync(
            CreateMaintenanceRecordDto dto,
            string techUser,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.DescripcionProblema) || string.IsNullOrWhiteSpace(dto.SolucionAplicada))
            {
                return new TechOperationResultDto
                {
                    Success = false,
                    Message = "Debe indicar la descripción del problema y la solución aplicada."
                };
            }

            var entity = new RegistroMantenimiento
            {
                Id = Guid.NewGuid(),
                FechaMantenimientoUtc = DateTime.UtcNow,
                Tipo = dto.Tipo,
                DescripcionProblema = SanitizeSensitiveText(dto.DescripcionProblema.Trim(), 1500),
                SolucionAplicada = SanitizeSensitiveText(dto.SolucionAplicada.Trim(), 2000),
                VersionSistema = string.IsNullOrWhiteSpace(dto.VersionSistema) ? GetCurrentSystemVersion() : dto.VersionSistema.Trim(),
                RespaldoPrevioRealizado = dto.RespaldoPrevioRealizado,
                Resultado = string.IsNullOrWhiteSpace(dto.Resultado) ? "Exitoso" : dto.Resultado.Trim(),
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : SanitizeSensitiveText(dto.Observaciones.Trim(), 1500),
                RegistradoPor = techUser,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            await _gateway.AddMaintenanceRecordAsync(entity, cancellationToken);
            await RecordAuditAsync(techUser, "Mantenimiento", $"Registrar Intervención ({dto.Tipo})", true, entity.DescripcionProblema, cancellationToken);

            return new TechOperationResultDto { Success = true, Message = "Intervención registrada correctamente en el historial de mantenimiento." };
        }

        public async Task<TechOperationResultDto> PrepareInnoSetupUpdateAsync(
            string versionDestino,
            string notas,
            string techUser,
            CancellationToken cancellationToken = default)
        {
            var currentVer = GetCurrentSystemVersion();
            var targetVer = string.IsNullOrWhiteSpace(versionDestino) ? currentVer : versionDestino.Trim();

            // 1. Crear respaldo obligatorio previo a la actualización
            var bkp = await CreateSafeBackupAsync(techUser, $"PRE_UPD_{targetVer}", null, false, cancellationToken);
            if (!bkp.Success)
            {
                return new TechOperationResultDto { Success = false, Message = $"Preparación cancelada: no se pudo crear respaldo previo ({bkp.Message})." };
            }

            // 2. Verificar integridad SQLite antes de ejecutar el nuevo instalador Inno Setup 6
            var integrity = await _gateway.RunIntegrityCheckAsync(cancellationToken);
            if (!integrity.IsValid)
            {
                return new TechOperationResultDto
                {
                    Success = false,
                    Message = $"Preparación detenida: la base de datos no superó PRAGMA integrity_check ({string.Join("; ", integrity.Results)})."
                };
            }

            // 3. Registrar en el Historial de Mantenimiento (versión anterior -> nueva)
            await _gateway.AddMaintenanceRecordAsync(new RegistroMantenimiento
            {
                Id = Guid.NewGuid(),
                FechaMantenimientoUtc = DateTime.UtcNow,
                Tipo = TipoMantenimiento.ActualizacionSistema,
                DescripcionProblema = $"Preparación segura de actualización mediante instalador Inno Setup 6 (v{currentVer} → v{targetVer}).",
                SolucionAplicada = $"Respaldo previo verificado ({Path.GetFileName(bkp.SecondaryDetail)}) e integridad SQLite confirmada ({integrity.DurationMs} ms). Base de datos y respaldos protegidos en C:\\ProgramData\\DC_BOUTIQUE.",
                VersionAnterior = currentVer,
                VersionSistema = targetVer,
                RespaldoPrevioRealizado = true,
                Resultado = "Listo para instalar",
                Observaciones = string.IsNullOrWhiteSpace(notas) ? "Ejecute el nuevo instalador Inno Setup 6 sin desinstalar los datos." : notas.Trim(),
                RegistradoPor = techUser,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            }, cancellationToken);

            await RecordAuditAsync(techUser, "Actualizaciones", $"Preparar Actualización Inno Setup (v{currentVer} → v{targetVer})", true, bkp.Message, cancellationToken);

            return new TechOperationResultDto
            {
                Success = true,
                Message = $"Sistema preparado para actualizar (v{currentVer} → v{targetVer}). Respaldo preventivo e integridad verificados y registrados en el historial."
            };
        }

        // =========================================================================
        // UTILIDADES INTERNAS (RETENCIÓN, DIAGNÓSTICOS, FORMATO)
        // =========================================================================
        private static void ApplyRetentionPolicy(string backupDir, ConfiguracionRespaldos cfg)
        {
            try
            {
                if (!Directory.Exists(backupDir)) return;

                var allFiles = new DirectoryInfo(backupDir)
                    .GetFiles("*.db")
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                // Nunca eliminar si solo queda 1 respaldo válido
                if (allFiles.Count <= 1) return;

                var keepSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Proteger siempre el archivo más reciente, manuales y PRE_RESTORE/PRE_UPD
                keepSet.Add(allFiles[0].FullName);
                foreach (var f in allFiles)
                {
                    if (f.Name.Contains("PRE_RESTORE", StringComparison.OrdinalIgnoreCase)
                        || f.Name.Contains("PRE_UPD", StringComparison.OrdinalIgnoreCase)
                        || f.Name.Contains("MANUAL", StringComparison.OrdinalIgnoreCase))
                    {
                        keepSet.Add(f.FullName);
                    }
                }

                var autoFiles = allFiles
                    .Where(f => f.Name.Contains("_AUTO_", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // 1. Conservar últimos N horarios
                foreach (var f in autoFiles.Take(Math.Max(1, cfg.RetenerHorarios)))
                {
                    keepSet.Add(f.FullName);
                }

                // 2. Conservar N diarios (1 por día)
                if (cfg.ConservarDiarios)
                {
                    var dailyKeep = autoFiles
                        .GroupBy(f => f.CreationTime.Date)
                        .OrderByDescending(g => g.Key)
                        .Take(Math.Max(1, cfg.RetenerDiarios))
                        .Select(g => g.First());
                    foreach (var f in dailyKeep) keepSet.Add(f.FullName);
                }

                // 3. Conservar N semanales (1 por semana ISO)
                if (cfg.ConservarSemanales)
                {
                    var weeklyKeep = autoFiles
                        .GroupBy(f => $"{ISOWeek.GetYear(f.CreationTime)}-W{ISOWeek.GetWeekOfYear(f.CreationTime)}")
                        .OrderByDescending(g => g.Max(x => x.CreationTime))
                        .Take(Math.Max(1, cfg.RetenerSemanales))
                        .Select(g => g.First());
                    foreach (var f in weeklyKeep) keepSet.Add(f.FullName);
                }

                // 4. Conservar N mensuales (1 por mes)
                if (cfg.ConservarMensuales)
                {
                    var monthlyKeep = autoFiles
                        .GroupBy(f => $"{f.CreationTime:yyyy-MM}")
                        .OrderByDescending(g => g.Key)
                        .Take(Math.Max(1, cfg.RetenerMensuales))
                        .Select(g => g.First());
                    foreach (var f in monthlyKeep) keepSet.Add(f.FullName);
                }

                var cutoffDate = DateTime.Now.AddDays(-Math.Max(1, cfg.RetencionDias));

                foreach (var f in autoFiles)
                {
                    if (keepSet.Contains(f.FullName) && f.CreationTime >= cutoffDate)
                    {
                        continue;
                    }

                    // Si supera la retención máxima o no cae en ninguna cubeta retenida, eliminar solo si no está bloqueado
                    if (!keepSet.Contains(f.FullName) && !IsFileLocked(f.FullName))
                    {
                        try { f.Delete(); } catch { }
                    }
                }
            }
            catch { }
        }

        private static List<TechBackupEntryDto> ScanBackupsInDirectory(string directory)
        {
            var list = new List<TechBackupEntryDto>();
            if (!Directory.Exists(directory)) return list;

            var files = new DirectoryInfo(directory)
                .GetFiles("*.db")
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            var validCount = files.Count(f => f.Length > 100);

            foreach (var f in files)
            {
                string origen = f.Name.Contains("PRE_RESTORE", StringComparison.OrdinalIgnoreCase) ? "Preventivo"
                    : f.Name.Contains("PRE_UPD", StringComparison.OrdinalIgnoreCase) ? "Pre-Actualización"
                    : f.Name.Contains("AUTO_DIARIO", StringComparison.OrdinalIgnoreCase) ? "Automático Diario"
                    : f.Name.Contains("_AUTO_", StringComparison.OrdinalIgnoreCase) ? "Automático"
                    : f.Name.Contains("PRUEBA", StringComparison.OrdinalIgnoreCase) ? "Prueba"
                    : "Manual";

                list.Add(new TechBackupEntryDto
                {
                    FileName = f.Name,
                    FullPath = f.FullName,
                    SizeInBytes = f.Length,
                    CreatedAt = f.CreationTime,
                    TipoOrigen = origen,
                    Estado = f.Length > 100 ? "Válido" : "Vacío / Revisar",
                    EsPreventivo = origen == "Preventivo" || origen == "Pre-Actualización",
                    EsUltimoValido = validCount <= 1 && f.Length > 100
                });
            }

            return list;
        }

        private static DiagnosticCheckItemDto BuildDirectoriesCheck(string backupDir)
        {
            var wwwrootPublished = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var wwwrootDev = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var wwwrootPath = Directory.Exists(wwwrootPublished) ? wwwrootPublished : wwwrootDev;

            var required = new[]
            {
                ("Datos SQLite", DatabasePathHelper.GetDataDirectory()),
                ("Respaldos", backupDir),
                ("Imágenes", DatabasePathHelper.GetImagesDirectory("productos")),
                ("Llaves Sesión", DatabasePathHelper.GetDataProtectionKeysDirectory()),
                ("Recursos Web (wwwroot)", wwwrootPath)
            };

            var missing = required.Where(r => !Directory.Exists(r.Item2)).Select(r => r.Item1).ToList();
            return new DiagnosticCheckItemDto
            {
                Nombre = "3. Existencia de Directorios Necesarios",
                Estado = missing.Count == 0 ? "Correcto" : "Error",
                Resultado = missing.Count == 0 ? $"{required.Length}/{required.Length} carpetas verificadas" : $"Faltan: {string.Join(", ", missing)}",
                DescripcionProblema = missing.Count == 0 ? "Todas las carpetas del sistema existen." : "Faltan carpetas requeridas para operar correctamente.",
                Recomendacion = missing.Count == 0 ? "Ninguna acción requerida." : "Ejecute el instalador de reparación o verifique los permisos en C:\\ProgramData\\DC_BOUTIQUE."
            };
        }

        private static DiagnosticCheckItemDto BuildWritePermissionsCheck(string backupDir)
        {
            var dirs = new[] { DatabasePathHelper.GetDataDirectory(), backupDir, DatabasePathHelper.GetImagesDirectory("productos") };
            var failed = new List<string>();
            foreach (var d in dirs)
            {
                try
                {
                    if (!Directory.Exists(d)) { failed.Add(d); continue; }
                    var probe = Path.Combine(d, $".dc_perm_{Guid.NewGuid():N}.tmp");
                    File.WriteAllText(probe, "ok");
                    File.Delete(probe);
                }
                catch
                {
                    failed.Add(d);
                }
            }

            return new DiagnosticCheckItemDto
            {
                Nombre = "4. Permisos de Escritura en Almacenamiento",
                Estado = failed.Count == 0 ? "Correcto" : "Error",
                Resultado = failed.Count == 0 ? "Lectura y escritura verificada en Data, Backups e Images" : $"Sin permiso en {failed.Count} ruta(s)",
                DescripcionProblema = failed.Count == 0 ? "El usuario actual de Windows puede escribir sin restricciones UAC." : $"Sin escritura en: {string.Join(", ", failed)}",
                Recomendacion = failed.Count == 0 ? "Ninguna acción requerida." : "Otorgue control total a Usuarios en C:\\ProgramData\\DC_BOUTIQUE."
            };
        }

        private static string ResolveSafeBackupDirectory(string? configured)
        {
            if (string.IsNullOrWhiteSpace(configured) || IsInsideProgramFiles(configured))
            {
                return DatabasePathHelper.GetBackupDirectory();
            }
            DatabasePathHelper.EnsureDirectoryExists(configured);
            return configured;
        }

        private static bool IsInsideProgramFiles(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
                var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                if (!string.IsNullOrWhiteSpace(pf) && full.StartsWith(pf, StringComparison.OrdinalIgnoreCase)) return true;
                if (!string.IsNullOrWhiteSpace(pf86) && full.StartsWith(pf86, StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch { }
            return false;
        }

        private static bool IsFileLocked(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch
            {
                return true;
            }
        }

        private static DateTime? ComputeNextScheduledBackupUtc(ConfiguracionRespaldos cfg)
        {
            DateTime? nextInterval = null;
            if (cfg.AutomaticosActivos && cfg.Frecuencia != FrecuenciaRespaldo.Desactivado)
            {
                var baseUtc = cfg.UltimoRespaldoExitosoUtc ?? DateTime.UtcNow;
                nextInterval = baseUtc.AddMinutes((int)cfg.Frecuencia);
                if (nextInterval < DateTime.UtcNow) nextInterval = DateTime.UtcNow.AddMinutes(1);
            }

            DateTime? nextDaily = null;
            if (cfg.RespaldoDiarioActivo
                && TimeSpan.TryParseExact(cfg.HoraRespaldoDiario, @"hh\:mm", CultureInfo.InvariantCulture, out var dailyTime))
            {
                var todayTarget = DateTime.Today.Add(dailyTime);
                bool ranToday = cfg.UltimoRespaldoDiarioUtc.HasValue && cfg.UltimoRespaldoDiarioUtc.Value.ToLocalTime().Date == DateTime.Today;
                var targetLocal = (DateTime.Now >= todayTarget && ranToday) ? todayTarget.AddDays(1) : todayTarget;
                nextDaily = targetLocal.ToUniversalTime();
            }

            if (nextInterval.HasValue && nextDaily.HasValue)
                return nextInterval.Value <= nextDaily.Value ? nextInterval : nextDaily;
            return nextInterval ?? nextDaily;
        }

        private static DateTime? ComputeNextScheduledBackupLocal(ConfiguracionRespaldos cfg)
        {
            var utc = ComputeNextScheduledBackupUtc(cfg);
            return utc?.ToLocalTime();
        }

        public static string FormatFrequency(FrecuenciaRespaldo f) => f switch
        {
            FrecuenciaRespaldo.Desactivado => "Desactivados",
            FrecuenciaRespaldo.Cada30Minutos => "Cada 30 minutos",
            FrecuenciaRespaldo.Cada1Hora => "Cada 1 hora",
            FrecuenciaRespaldo.Cada2Horas => "Cada 2 horas",
            FrecuenciaRespaldo.Cada4Horas => "Cada 4 horas",
            FrecuenciaRespaldo.Cada6Horas => "Cada 6 horas",
            FrecuenciaRespaldo.Cada12Horas => "Cada 12 horas",
            FrecuenciaRespaldo.Diario => "Una vez al día",
            _ => f.ToString()
        };

        public static string FormatMaintenanceType(TipoMantenimiento t) => t switch
        {
            TipoMantenimiento.CorreccionErrores => "Corrección de errores",
            TipoMantenimiento.ActualizacionSistema => "Actualización del sistema",
            TipoMantenimiento.RestauracionRespaldo => "Restauración de respaldo",
            TipoMantenimiento.MantenimientoPreventivo => "Mantenimiento preventivo",
            TipoMantenimiento.ConfiguracionImpresora => "Configuración de impresora",
            TipoMantenimiento.Diagnostico => "Diagnóstico",
            TipoMantenimiento.ModificacionTecnica => "Modificación técnica",
            _ => "Otro"
        };

        private static TechBackupConfigDto MapBackupConfig(ConfiguracionRespaldos c, string resolvedDir) => new()
        {
            Id = c.Id,
            AutomaticosActivos = c.AutomaticosActivos,
            Frecuencia = c.Frecuencia,
            RespaldoDiarioActivo = c.RespaldoDiarioActivo,
            HoraRespaldoDiario = c.HoraRespaldoDiario,
            CarpetaDestino = resolvedDir,
            RetenerHorarios = c.RetenerHorarios,
            RetenerDiarios = c.RetenerDiarios,
            RetenerSemanales = c.RetenerSemanales,
            RetenerMensuales = c.RetenerMensuales,
            RetencionDias = c.RetencionDias,
            ConservarDiarios = c.ConservarDiarios,
            ConservarSemanales = c.ConservarSemanales,
            ConservarMensuales = c.ConservarMensuales,
            UltimoRespaldoExitosoLocal = c.UltimoRespaldoExitosoUtc?.ToLocalTime(),
            UltimoArchivoRespaldo = c.UltimoArchivoRespaldo,
            ProximoRespaldoProgramadoLocal = ComputeNextScheduledBackupLocal(c),
            UltimoErrorRespaldo = c.UltimoErrorRespaldo,
            FechaUltimoErrorLocal = c.FechaUltimoErrorUtc?.ToLocalTime()
        };

        private static RegistroLogTecnicoDto MapLog(RegistroLogTecnico l) => new()
        {
            Id = l.Id,
            FechaEventoUtc = l.FechaEvento,
            Tipo = l.Tipo,
            Severidad = l.Severidad,
            Origen = l.Origen,
            Mensaje = l.Mensaje,
            DetalleTecnico = l.DetalleTecnico,
            UsuarioRelacionado = l.UsuarioRelacionado
        };

        private static AuditoriaTecnicaDto MapAudit(AuditoriaTecnica a) => new()
        {
            Id = a.Id,
            FechaAccionUtc = a.FechaAccion,
            UsuarioTecnico = a.UsuarioTecnico,
            Modulo = a.Modulo,
            Accion = a.Accion,
            Exitosa = a.Exitosa,
            Detalle = a.Detalle
        };

        private static RegistroMantenimientoDto MapMaintenance(RegistroMantenimiento m) => new()
        {
            Id = m.Id,
            FechaMantenimientoUtc = m.FechaMantenimientoUtc,
            Tipo = m.Tipo,
            DescripcionProblema = m.DescripcionProblema,
            SolucionAplicada = m.SolucionAplicada,
            VersionSistema = m.VersionSistema,
            VersionAnterior = m.VersionAnterior,
            RespaldoPrevioRealizado = m.RespaldoPrevioRealizado,
            Resultado = m.Resultado,
            Observaciones = m.Observaciones,
            RegistradoPor = m.RegistradoPor
        };

        private static string SanitizeSensitiveText(string? input, int maxLen)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var cleaned = SensitivePairRegex.Replace(input, "$1=[REDACTED]");
            return cleaned.Length > maxLen ? cleaned[..maxLen] : cleaned;
        }

        private static (string DriveName, long FreeBytes, long TotalBytes, double UsedPct) GetDiskSpaceMetrics(string path)
        {
            try
            {
                var root = Path.GetPathRoot(path);
                if (string.IsNullOrWhiteSpace(root)) root = @"C:\";
                var drive = new DriveInfo(root);
                if (drive.IsReady && drive.TotalSize > 0)
                {
                    double usedPct = Math.Round((double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize * 100.0, 1);
                    return (drive.Name, drive.AvailableFreeSpace, drive.TotalSize, usedPct);
                }
            }
            catch { }
            return ("C:\\", 0, 0, 0);
        }

        private static EnvironmentInfoDto BuildEnvironmentInfo()
        {
            using var proc = Process.GetCurrentProcess();
            return new EnvironmentInfoDto
            {
                MachineName = Environment.MachineName,
                OsDescription = RuntimeInformation.OSDescription,
                OsArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                DotNetVersion = RuntimeInformation.FrameworkDescription,
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                BaseDirectory = AppContext.BaseDirectory,
                ProgramDataDirectory = DatabasePathHelper.GetBaseApplicationDataDirectory(),
                ProcessorCount = Environment.ProcessorCount,
                WorkingSetMb = proc.WorkingSet64 / (1024 * 1024),
                TimeZoneName = TimeZoneInfo.Local.DisplayName,
                ServerLocalTime = DateTime.Now
            };
        }

        private static void AddJsonEntry(ZipArchive zip, string entryName, object data, JsonSerializerOptions options)
        {
            var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(JsonSerializer.Serialize(data, options));
        }

        private static string CsvEscape(string value)
        {
            var clean = value.Replace("\r", " ").Replace("\n", " ");
            return $"\"{clean.Replace("\"", "\"\"")}\"";
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            double kb = bytes / 1024.0;
            if (kb < 1024) return $"{kb:F1} KB";
            double mb = kb / 1024.0;
            if (mb < 1024) return $"{mb:F2} MB";
            return $"{mb / 1024.0:F2} GB";
        }
    }
}
