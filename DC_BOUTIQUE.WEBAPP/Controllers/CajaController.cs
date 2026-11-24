using DC_BOUTIQUE.Core.Application.DTOs.Caja;
using DC_BOUTIQUE.Core.Application.Extensions;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.ViewModels.Caja;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    public class CajaController : Controller
    {
        private readonly ICajaService _cajaService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITicketPrintingService _ticketPrintingService;

        public CajaController(ICajaService cajaService, IUnitOfWork unitOfWork, ITicketPrintingService ticketPrintingService)
        {
            _cajaService = cajaService;
            _unitOfWork = unitOfWork;
            _ticketPrintingService = ticketPrintingService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var adminUser = await GetCurrentUserIdAsync();
            var turnoDto = await _cajaService.GetTurnoAbiertoAsync(adminUser);

            if (turnoDto == null)
            {
                return RedirectToAction(nameof(Abrir));
            }

            var vm = turnoDto.ToViewModel();
            ViewBag.TurnoAbierto = vm;
            var gastosDto = await _cajaService.GetGastosPorTurnoAsync(turnoDto.Id);
            ViewBag.Gastos = gastosDto.Select(g => g.ToViewModel()).ToList();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Abrir()
        {
            var adminUser = await GetCurrentUserIdAsync();
            var turnoAbierto = await _cajaService.GetTurnoAbiertoAsync(adminUser);
            if (turnoAbierto != null)
            {
                TempData["SuccessMessage"] = "La caja ya se encuentra abierta y lista para facturar.";
                return RedirectToAction("Index", "Pos");
            }

            var todos = await _unitOfWork.TurnosCaja.GetAllListAsync();
            var ultimoCerrado = todos
                .Where(t => t.Estado != Core.Domain.Common.Enums.EstadoCaja.Abierta && t.FechaCierre.HasValue)
                .OrderByDescending(t => t.FechaCierre)
                .FirstOrDefault();

            if (ultimoCerrado != null)
            {
                ViewBag.UltimoTurno = ultimoCerrado.ToDto().ToViewModel();
            }

            return View("Apertura", new AbrirTurnoViewModel { FondoInicialEfectivo = 0m });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Abrir(AbrirTurnoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                TempData["ErrorMessage"] = $"Por favor complete los campos requeridos:<br>• {string.Join("<br>• ", errors)}";
                return View("Apertura", model);
            }

            var userId = await GetCurrentUserIdAsync();
            var dto = model.ToDto();
            var result = await _cajaService.AbrirTurnoAsync(userId, dto);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                return View("Apertura", model);
            }

            TempData["SuccessMessage"] = "Turno de caja abierto correctamente. ¡Listo para facturar!";
            return RedirectToAction("Index", "Pos");
        }

        [HttpGet]
        public async Task<IActionResult> Cerrar()
        {
            var userId = await GetCurrentUserIdAsync();
            var turnoDto = await _cajaService.GetTurnoAbiertoAsync(userId);

            if (turnoDto == null)
            {
                TempData["ErrorMessage"] = "No hay ningún turno de caja abierto actualmente.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CerrarTurnoViewModel
            {
                TurnoId = turnoDto.Id,
                EfectivoContadoAlCierre = turnoDto.EfectivoEsperadoEnCaja
            };

            ViewBag.Turno = turnoDto.ToViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(CerrarTurnoViewModel model)
        {
            var userId = await GetCurrentUserIdAsync();
            var turnoDto = await _cajaService.GetTurnoAbiertoAsync(userId);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                TempData["ErrorMessage"] = $"Por favor verifique los datos del arqueo:<br>• {string.Join("<br>• ", errors)}";
                ViewBag.Turno = turnoDto != null ? turnoDto.ToViewModel() : null;
                return View(model);
            }

            var dto = model.ToDto();
            var result = await _cajaService.CerrarTurnoAsync(userId, dto);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                ViewBag.Turno = turnoDto != null ? turnoDto.ToViewModel() : null;
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> RegistrarGasto()
        {
            var userId = await GetCurrentUserIdAsync();
            var turnoDto = await _cajaService.GetTurnoAbiertoAsync(userId);

            if (turnoDto == null)
            {
                TempData["ErrorMessage"] = "Debe haber una caja abierta para registrar egresos.";
                return RedirectToAction(nameof(Index));
            }

            return View(new SaveGastoCajaViewModel { TurnoCajaId = turnoDto.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarGasto(SaveGastoCajaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                TempData["ErrorMessage"] = $"Por favor verifique los campos del gasto:<br>• {string.Join("<br>• ", errors)}";
                return View(model);
            }

            var userId = await GetCurrentUserIdAsync();
            var dto = model.ToDto();
            var result = await _cajaService.RegistrarGastoAsync(userId, dto);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Historial(DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var turnosDto = await _cajaService.GetHistorialTurnosAsync(fechaDesde, fechaHasta);
            var model = turnosDto.Select(t => t.ToViewModel()).ToList();

            ViewBag.FechaDesde = fechaDesde?.ToString("yyyy-MM-dd");
            ViewBag.FechaHasta = fechaHasta?.ToString("yyyy-MM-dd");

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ImprimirCierre(Guid? id)
        {
            try
            {
                TurnoCajaDto? turnoDto = null;
                if (id.HasValue && id.Value != Guid.Empty)
                {
                    turnoDto = await _cajaService.GetTurnoPorIdAsync(id.Value);
                }

                if (turnoDto == null)
                {
                    // Fallback al último turno cerrado si no se especificó ID o no fue encontrado
                    var todos = await _unitOfWork.TurnosCaja.GetAllListAsync();
                    var ultimoCerrado = todos
                        .Where(t => t.Estado != Core.Domain.Common.Enums.EstadoCaja.Abierta && t.FechaCierre.HasValue)
                        .OrderByDescending(t => t.FechaCierre)
                        .FirstOrDefault();

                    if (ultimoCerrado != null)
                    {
                        turnoDto = await _cajaService.GetTurnoPorIdAsync(ultimoCerrado.Id);
                    }
                }

                if (turnoDto == null)
                {
                    return Content("<div style='font-family:sans-serif;padding:30px;text-align:center;'><h3>No se encontró comprobante de turno de caja</h3><p>No hay registro de cierre previo disponible para imprimir.</p></div>", "text/html; charset=utf-8");
                }

                string html = _ticketPrintingService.GenerateCierreCajaTicketHtml(turnoDto);
                return Content(html, "text/html; charset=utf-8");
            }
            catch (Exception ex)
            {
                return Content($"<div style='font-family:sans-serif;padding:30px;text-align:center;color:#b91c1c;'><h3>Error al generar comprobante</h3><p>{ex.Message}</p></div>", "text/html; charset=utf-8");
            }
        }

        private async Task<Guid> GetCurrentUserIdAsync()
        {
            try
            {
                var userName = HttpContext.Session.GetString("UserName");
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    var user = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync(userName.Trim());
                    if (user != null) return user.Id;
                }

                var sessionUserId = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(sessionUserId) && Guid.TryParse(sessionUserId, out Guid guid))
                {
                    var userExists = await _unitOfWork.Usuarios.GetByIdAsync(guid);
                    if (userExists != null) return guid;
                }

                var admin = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync("admin");
                if (admin != null) return admin.Id;

                var anyUser = await _unitOfWork.Usuarios.GetAllListAsync();
                if (anyUser.Any()) return anyUser.First().Id;

                var defaultAdmin = new DC_BOUTIQUE.Core.Domain.Entities.Usuario
                {
                    Id = Guid.NewGuid(),
                    NombreCompleto = "Administrador Sistema",
                    NombreUsuario = "admin",
                    PasswordHash = "DEFAULT_SYSTEM_HASH",
                    Rol = DC_BOUTIQUE.Core.Domain.Common.Enums.RolUsuario.Administrador,
                    Activo = true
                };
                await _unitOfWork.Usuarios.AddAsync(defaultAdmin);
                await _unitOfWork.SaveChangesAsync();
                return defaultAdmin.Id;
            }
            catch
            {
                return Guid.Empty;
            }
        }
    }
}
