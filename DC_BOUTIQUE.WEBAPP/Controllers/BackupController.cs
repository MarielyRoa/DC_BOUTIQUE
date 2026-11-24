using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.WEBAPP.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    [AuthorizeAdmin]
    public class BackupController : Controller
    {
        private readonly IDatabaseBackupService _backupService;

        public BackupController(IDatabaseBackupService backupService)
        {
            _backupService = backupService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var backups = await _backupService.GetAvailableBackupsAsync();
                return View(backups);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al consultar respaldos: {ex.Message}";
                return View(new List<BackupFileInfo>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarBackup()
        {
            try
            {
                var result = await _backupService.CreateBackupAsync("Backup manual desde interfaz administrativa");
                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Copia de seguridad '{result.FileName}' generada exitosamente ({Math.Round((double)result.FileSizeBytes / (1024 * 1024), 2)} MB).";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"No se pudo generar el backup: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(string fileName)
        {
            try
            {
                bool ok = await _backupService.DeleteBackupAsync(fileName);
                if (ok)
                {
                    TempData["SuccessMessage"] = "Copia de seguridad eliminada.";
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo eliminar el archivo de copia de seguridad.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Descargar(string fileName)
        {
            try
            {
                string safeFileName = Path.GetFileName(fileName);
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Backups", safeFileName);

                if (!System.IO.File.Exists(fullPath))
                {
                    TempData["ErrorMessage"] = "El archivo solicitado no existe en el almacenamiento.";
                    return RedirectToAction(nameof(Index));
                }

                var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                return File(bytes, "application/octet-stream", safeFileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al descargar el archivo: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
