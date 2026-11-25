using System.Diagnostics;
using System.Net;
using System.Text.Json;
using DC_BOUTIQUE.Core.Application.DTOs.Account;
using DC_BOUTIQUE.Core.Application.DTOs.TechAdmin;
using DC_BOUTIQUE.Core.Application.DTOs.Usuarios;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.ViewModels.TechAdmin;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.WEBAPP.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    /// <summary>
    /// Controlador exclusivo del Centro del Desarrollador — DC_BOUTIQUE.
    /// Protegido en servidor por [AuthorizeDeveloper] (rol Desarrollador).
    /// </summary>
    public class TechAdminController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IAccountService _accountService;
        private readonly ITechAdminService _techService;

        public TechAdminController(
            IUsuarioService usuarioService,
            IAccountService accountService,
            ITechAdminService techService)
        {
            _usuarioService = usuarioService;
            _accountService = accountService;
            _techService = techService;
        }

        private string CurrentTechUser => HttpContext.Session.GetString("UserName") ?? "Desarrollador";

        // =========================================================================
        // CONFIGURACIÓN INICIAL SEGURA DE LA CUENTA DESARROLLADOR (LOCALHOST ONLY)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Setup()
        {
            if (!IsLocalLoopbackRequest()) return Forbid();
            if (await _usuarioService.ExisteDesarrolladorAsync()) return NotFound();
            return View(new SetupDesarrolladorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(SetupDesarrolladorViewModel model)
        {
            if (!IsLocalLoopbackRequest()) return Forbid();
            if (await _usuarioService.ExisteDesarrolladorAsync()) return NotFound();
            if (!ModelState.IsValid) return View(model);

            try
            {
                var nombreLimpio = model.NombreCompleto.Trim();
                var usuarioLimpio = model.NombreUsuario.Trim();
                var partes = nombreLimpio.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var firstName = partes.Length > 0 ? partes[0] : nombreLimpio;
                var lastName = partes.Length > 1 ? partes[1] : "Técnico";

                var identityResult = await _accountService.RegisterDeveloperInitialAsync(new RegisterRequest
                {
                    FirstName = firstName,
                    LastName = lastName,
                    UserName = usuarioLimpio,
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword,
                    Role = "Desarrollador"
                });

                if (identityResult.HasError)
                {
                    ModelState.AddModelError(string.Empty, identityResult.Error ?? "No se pudo crear la cuenta en Identity.");
                    return View(model);
                }

                var domainResult = await _usuarioService.CrearDesarrolladorInicialAsync(new SaveUsuarioDto
                {
                    NombreCompleto = nombreLimpio,
                    NombreUsuario = usuarioLimpio,
                    Password = model.Password,
                    Rol = RolUsuario.Desarrollador,
                    Activo = true
                });

                if (!domainResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, domainResult.Message ?? "No se pudo registrar el perfil técnico.");
                    return View(model);
                }

                await _techService.RecordAuditAsync(
                    usuarioLimpio,
                    "Seguridad",
                    "Configuración Inicial de Cuenta Desarrollador",
                    true,
                    "Cuenta inicial creada localmente y ruta /TechAdmin/Setup bloqueada.");

                TempData["SuccessMessage"] = "Cuenta Desarrollador creada correctamente. Inicie sesión con sus credenciales.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // =========================================================================
        // 1. DASHBOARD TÉCNICO
        // =========================================================================
        [AuthorizeDeveloper]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard Técnico";
            var model = await _techService.GetDashboardAsync();
            return View(model);
        }

        // =========================================================================
        // 2. BASE DE DATOS Y RESPALDOS (MANUALES, AUTOMÁTICOS Y RESTAURACIÓN)
        // =========================================================================
        [AuthorizeDeveloper]
        [HttpGet]
        public async Task<IActionResult> BaseDatos()
        {
            ViewData["Title"] = "Base de Datos y Respaldos";
            var model = await _techService.GetDatabaseStatusAsync();
            if (TempData["IntegrityReportJson"] is string reportJson)
            {
                ViewBag.IntegrityReport = JsonSerializer.Deserialize<TechIntegrityResultDto>(reportJson);
            }
            return View(model);
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerificarIntegridad()
        {
            var report = await _techService.VerifyIntegrityAsync(CurrentTechUser);
            TempData["IntegrityReportJson"] = JsonSerializer.Serialize(report);

            if (report.IsValid)
                TempData["TechSuccess"] = $"Integridad SQLite verificada: PRAGMA integrity_check = ok ({report.DurationMs} ms).";
            else
                TempData["TechError"] = $"Inconsistencias detectadas en SQLite: {string.Join("; ", report.Messages)}";

            return RedirectToAction(nameof(BaseDatos));
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearRespaldo(string? nota, string? carpetaDestinoCustom)
        {
            var res = await _techService.CreateSafeBackupAsync(CurrentTechUser, nota, carpetaDestinoCustom, isTest: false);
            if (res.Success) TempData["TechSuccess"] = res.Message;
            else TempData["TechError"] = res.Message;
            return RedirectToAction(nameof(BaseDatos));
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EjecutarRespaldoPrueba()
        {
            var res = await _techService.CreateSafeBackupAsync(CurrentTechUser, "PRUEBA", null, isTest: true);
            if (res.Success) TempData["TechSuccess"] = res.Message;
            else TempData["TechError"] = res.Message;
            return RedirectToAction(nameof(BaseDatos));
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarConfiguracionRespaldos(TechBackupConfigDto model)
        {
            var res = await _techService.SaveBackupConfigAsync(model, CurrentTechUser);
            if (res.Success) TempData["TechSuccess"] = res.Message;
            else TempData["TechError"] = res.Message;
            return RedirectToAction(nameof(BaseDatos));
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarRespaldo(string fileName)
        {
            var res = await _techService.DeleteBackupSafelyAsync(fileName, CurrentTechUser);
            if (res.Success) TempData["TechSuccess"] = res.Message;
            else TempData["TechError"] = res.Message;
            return RedirectToAction(nameof(BaseDatos));
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestaurarRespaldo(string fileName, string confirmText)
        {
            if (!string.Equals(confirmText?.Trim(), "RESTAURAR", StringComparison.Ordinal))
            {
                TempData["TechError"] = "Confirmación inválida. Debe escribir exactamente RESTAURAR para proceder.";
                return RedirectToAction(nameof(BaseDatos));
            }

            var res = await _techService.RestoreBackupSafelyAsync(fileName, CurrentTechUser);
            if (res.Success) TempData["TechSuccess"] = res.Message;
            else TempData["TechError"] = res.Message;
            return RedirectToAction(nameof(BaseDatos));
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AbrirCarpetaRespaldos()
        {
            try
            {
                var dir = await _techService.GetConfiguredBackupDirectoryAsync();
                if (OperatingSystem.IsWindows() && Directory.Exists(dir))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
                    TempData["TechSuccess"] = $"Carpeta abierta en el Explorador de Windows: {dir}";
                }
                else
                {
                    TempData["TechError"] = $"La carpeta no está disponible localmente: {dir}";
                }
            }
            catch (Exception ex)
            {
                TempData["TechError"] = $"No se pudo abrir la carpeta en Windows: {ex.Message}";
            }
            return RedirectToAction(nameof(BaseDatos));
        }

        [AuthorizeDeveloper]
        [HttpGet]
        public async Task<IActionResult> ExportarInfoTecnica()
        {
            var (content, fileName) = await _techService.ExportTechnicalDbReportJsonAsync(CurrentTechUser);
            return File(content, "application/json; charset=utf-8", fileName);
        }

        // =========================================================================
        // 3. USUARIOS Y ACCESOS TÉCNICOS
        // =========================================================================
        [AuthorizeDeveloper]
        [HttpGet]
        public async Task<IActionResult> Usuarios()
        {
            ViewData["Title"] = "Usuarios y Accesos";
            var model = await _techService.GetUsersAndAccessAsync();
            return View(model);
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoUsuario(Guid usuarioId, bool activar)
        {
            var res = await _techService.ToggleUserActiveTechAsync(usuarioId, activar, CurrentTechUser);
            if (res.Success) TempData["TechSuccess"] = res.Message;
            else TempData["TechError"] = res.Message;
            return RedirectToAction(nameof(Usuarios));
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerPasswordUsuario(Guid usuarioId, string nuevaPassword, string confirmarPassword)
        {
            if (nuevaPassword != confirmarPassword)
            {
                TempData["TechError"] = "Las contraseñas ingresadas no coinciden.";
                return RedirectToAction(nameof(Usuarios));
            }

            var res = await _techService.ResetUserPasswordTechAsync(usuarioId, nuevaPassword, CurrentTechUser);
            if (res.Success) TempData["TechSuccess"] = res.Message;
            else TempData["TechError"] = res.Message;
            return RedirectToAction(nameof(Usuarios));
        }

        // =========================================================================
        // 4. CONFIGURACIÓN TÉCNICA + IMPRESIÓN DE PRUEBA
        // =========================================================================
        [AuthorizeDeveloper]
        [HttpGet]
        public async Task<IActionResult> Configuracion()
        {
            ViewData["Title"] = "Configuración Técnica";
            var model = await _techService.GetTechnicalConfigAsync();
            return View(model);
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarConfiguracionTecnica(TechConfigViewDto model)
        {
            var res = await _techService.SaveTechnicalConfigAsync(model, CurrentTechUser);
            if (res.Success) TempData["TechSuccess"] = res.Message;
            else TempData["TechError"] = res.Message;
            return RedirectToAction(nameof(Configuracion));
        }

        [AuthorizeDeveloper]
        [HttpGet]
        public async Task<IActionResult> ImpresionPrueba()
        {
            var html = await _techService.GenerateTestTicketHtmlAsync(CurrentTechUser);
            return Content(html, "text/html; charset=utf-8");
        }

        // =========================================================================
        // 5. LOGS Y ERRORES DEL SISTEMA
        // =========================================================================
        [AuthorizeDeveloper]
        [HttpGet]
        public async Task<IActionResult> Logs(DateTime? desde, DateTime? hasta, TipoLogTecnico? tipo, string? severidad, string? busqueda)
        {
            ViewData["Title"] = "Logs y Errores del Sistema";
            var model = await _techService.QueryLogsAndAuditsAsync(desde, hasta, tipo, severidad, busqueda);
            return View(model);
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LimpiarLogsAntiguos(int diasAntiguedad, string confirmText)
        {
            if (!string.Equals(confirmText?.Trim(), "LIMPIAR", StringComparison.Ordinal))
            {
                TempData["TechError"] = "Debe escribir exactamente LIMPIAR para confirmar la depuración de logs antiguos.";
                return RedirectToAction(nameof(Logs));
            }

            var res = await _techService.PurgeOldLogsAsync(diasAntiguedad, CurrentTechUser);
            if (res.Success) TempData["TechSuccess"] = res.Message;
            else TempData["TechError"] = res.Message;
            return RedirectToAction(nameof(Logs));
        }

        [AuthorizeDeveloper]
        [HttpGet]
        public async Task<IActionResult> ExportarLogs(string format, DateTime? desde, DateTime? hasta, TipoLogTecnico? tipo, string? severidad, string? busqueda)
        {
            var (content, contentType, fileName) = await _techService.ExportLogsAsync(
                format, desde, hasta, tipo, severidad, busqueda, CurrentTechUser);
            return File(content, contentType, fileName);
        }

        // =========================================================================
        // 6. DIAGNÓSTICO DEL SISTEMA (9 COMPROBACIONES + RECOMENDACIONES)
        // =========================================================================
        [AuthorizeDeveloper]
        [HttpGet]
        public async Task<IActionResult> Diagnostico()
        {
            ViewData["Title"] = "Diagnóstico del Sistema";
            var model = await _techService.RunFullDiagnosticsAsync(CurrentTechUser);
            return View(model);
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReejecutarDiagnostico()
        {
            var model = await _techService.RunFullDiagnosticsAsync(CurrentTechUser);
            if (model.TotalErrores == 0 && model.TotalAdvertencias == 0)
            {
                TempData["TechSuccess"] = $"Diagnóstico re-ejecutado correctamente ({DateTime.Now:HH:mm:ss}) — {model.TotalCorrectas}/{model.Comprobaciones.Count} comprobaciones en estado Correcto.";
            }
            else
            {
                TempData["TechError"] = $"Diagnóstico completado ({DateTime.Now:HH:mm:ss}) — Correctas: {model.TotalCorrectas}, Advertencias: {model.TotalAdvertencias}, Errores: {model.TotalErrores}.";
            }
            return RedirectToAction(nameof(Diagnostico));
        }

        [AuthorizeDeveloper]
        [HttpGet]
        public async Task<IActionResult> ExportarPaqueteDiagnostico()
        {
            var (zipBytes, fileName) = await _techService.GenerateDiagnosticPackageZipAsync(CurrentTechUser);
            return File(zipBytes, "application/zip", fileName);
        }

        // =========================================================================
        // 7. HISTORIAL DE MANTENIMIENTO + PROCEDIMIENTO ACTUALIZACIÓN INNO SETUP 6
        // =========================================================================
        [AuthorizeDeveloper]
        [HttpGet]
        public async Task<IActionResult> Mantenimiento(DateTime? desde, DateTime? hasta, TipoMantenimiento? tipo)
        {
            ViewData["Title"] = "Historial de Mantenimiento";
            var model = await _techService.GetMaintenanceHistoryAsync(desde, hasta, tipo);
            return View(model);
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarMantenimiento(CreateMaintenanceRecordDto model)
        {
            var res = await _techService.AddMaintenanceRecordAsync(model, CurrentTechUser);
            if (res.Success) TempData["TechSuccess"] = res.Message;
            else TempData["TechError"] = res.Message;
            return RedirectToAction(nameof(Mantenimiento));
        }

        [AuthorizeDeveloper]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrepararActualizacionInnoSetup(string versionDestino, string? notas)
        {
            var res = await _techService.PrepareInnoSetupUpdateAsync(versionDestino, notas ?? string.Empty, CurrentTechUser);
            if (res.Success) TempData["TechSuccess"] = res.Message;
            else TempData["TechError"] = res.Message;
            return RedirectToAction(nameof(Mantenimiento));
        }

        private bool IsLocalLoopbackRequest()
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            return remoteIp == null || IPAddress.IsLoopback(remoteIp);
        }
    }
}
