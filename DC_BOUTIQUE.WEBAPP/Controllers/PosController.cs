using DC_BOUTIQUE.Core.Application.DTOs.Ventas;
using DC_BOUTIQUE.Core.Application.Extensions;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.ViewModels.Ventas;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.WEBAPP.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    [AuthorizeSession]
    public class PosController : Controller
    {
        private readonly IVentaService _ventaService;
        private readonly IProductoService _productoService;
        private readonly ICajaService _cajaService;
        private readonly IClienteService _clienteService;
        private readonly ITicketPrintingService _ticketPrintingService;
        private readonly IUnitOfWork _unitOfWork;

        public PosController(
            IVentaService ventaService,
            IProductoService productoService,
            ICajaService cajaService,
            IClienteService clienteService,
            ITicketPrintingService ticketPrintingService,
            IUnitOfWork unitOfWork)
        {
            _ventaService = ventaService;
            _productoService = productoService;
            _cajaService = cajaService;
            _clienteService = clienteService;
            _ticketPrintingService = ticketPrintingService;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = await GetCurrentUserIdAsync();
            var turnoAbierto = await _cajaService.GetTurnoAbiertoAsync(userId);

            if (turnoAbierto == null)
            {
                TempData["ErrorMessage"] = "No hay un turno de caja abierto para facturar. Por favor ingrese el fondo inicial para aperturar la caja.";
                return RedirectToAction("Abrir", "Caja");
            }

            ViewBag.Turno = turnoAbierto.ToViewModel();

            var clientes = await _clienteService.GetAllAsync();
            ViewBag.Clientes = clientes.Select(c => c.ToViewModel()).ToList();

            var clienteFinal = clientes.FirstOrDefault(c => c.Nombres.Contains("Consumidor") || c.Nombres.Contains("Final"))
                ?? clientes.FirstOrDefault();
            ViewBag.ClientePredeterminado = clienteFinal != null ? clienteFinal.ToViewModel() : null;

            var productos = await _productoService.GetAllAsync();
            var model = productos.Select(p => p.ToViewModel()).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarPrenda(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
            {
                return Json(new { success = false, message = "Debe ingresar un término de búsqueda." });
            }

            var clean = termino.Trim();

            // 1. Búsqueda exacta por Código de Barras
            var porCodigoBarras = await _productoService.GetVarianteByCodigoBarrasAsync(clean);
            if (porCodigoBarras != null && porCodigoBarras.Activo)
            {
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        varianteId = porCodigoBarras.Id,
                        nombre = porCodigoBarras.NombreProducto,
                        sku = porCodigoBarras.Sku,
                        talla = porCodigoBarras.Talla,
                        color = porCodigoBarras.Color,
                        precio = porCodigoBarras.PrecioEfectivo,
                        stock = porCodigoBarras.StockActual,
                        codigoBarras = porCodigoBarras.CodigoBarras
                    }
                });
            }

            // 2. Búsqueda exacta por SKU
            var porSku = await _productoService.GetVarianteBySkuAsync(clean);
            if (porSku != null && porSku.Activo)
            {
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        varianteId = porSku.Id,
                        nombre = porSku.NombreProducto,
                        sku = porSku.Sku,
                        talla = porSku.Talla,
                        color = porSku.Color,
                        precio = porSku.PrecioEfectivo,
                        stock = porSku.StockActual,
                        codigoBarras = porSku.CodigoBarras
                    }
                });
            }

            // 3. Búsqueda predictiva por nombre o modelo
            var productos = await _productoService.GetAllAsync();
            var coincidentes = productos
                .Where(p => p.Activo && (
                    p.Nombre.Contains(clean, StringComparison.OrdinalIgnoreCase) ||
                    p.CodigoModelo.Contains(clean, StringComparison.OrdinalIgnoreCase)))
                .SelectMany(p => p.Variantes.Where(v => v.Activo).Select(v => new
                {
                    varianteId = v.Id,
                    nombre = p.Nombre,
                    sku = v.Sku,
                    talla = v.Talla,
                    color = v.Color,
                    precio = v.PrecioEfectivo,
                    stock = v.StockActual,
                    codigoBarras = v.CodigoBarras
                }))
                .Take(10)
                .ToList();

            if (coincidentes.Any())
            {
                return Json(new { success = true, multiples = true, data = coincidentes });
            }

            return Json(new { success = false, message = $"No se encontró ninguna prenda con el código '{clean}'." });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ProcesarVenta([FromBody] RegistrarVentaDto model)
        {
            if (model == null || model.Items == null || model.Items.Count == 0)
            {
                return Json(new { success = false, message = "El ticket está vacío. Agregue al menos una prenda antes de cobrar." });
            }

            if (model.Pagos == null || model.Pagos.Count == 0)
            {
                return Json(new { success = false, message = "Debe indicar al menos un método de pago." });
            }

            var userId = await GetCurrentUserIdAsync();
            var turno = await _cajaService.GetTurnoAbiertoAsync(userId);
            if (turno == null)
            {
                return Json(new { success = false, message = "No hay caja abierta para procesar la venta. Por favor abra un turno primero." });
            }

            model.TurnoCajaId = turno.Id;
            var result = await _ventaService.RegistrarVentaAsync(userId, model);

            if (!result.Succeeded || result.Data == null)
            {
                return Json(new { success = false, message = result.Message });
            }

            var ventaRealizada = result.Data;
            string ticketHtml = _ticketPrintingService.GenerateTicketHtml(ventaRealizada);

            return Json(new
            {
                success = true,
                numeroFactura = ventaRealizada.NumeroFactura,
                total = ventaRealizada.TotalNeto,
                ticketHtml = ticketHtml,
                itemsVendidos = model.Items.Select(i => new
                {
                    productoVarianteId = i.ProductoVarianteId,
                    cantidad = i.Cantidad
                })
            });
        }

        [HttpGet]
        public async Task<IActionResult> ImprimirTicket(Guid id)
        {
            var venta = await _ventaService.GetByIdAsync(id);
            if (venta == null) return NotFound();

            string html = _ticketPrintingService.GenerateTicketHtml(venta);
            return Content(html, "text/html");
        }

        [HttpGet]
        public async Task<IActionResult> Ticket(Guid id)
        {
            return await ImprimirTicket(id);
        }

        [HttpGet]
        public async Task<IActionResult> Ventas(DateTime? fecha)
        {
            var targetFecha = fecha ?? DC_BOUTIQUE.Core.Application.Helpers.DominicanDateTime.Now.Date;
            var ventas = await _ventaService.GetVentasDelDiaAsync(targetFecha);
            var model = ventas.Select(v => v.ToViewModel()).ToList();

            ViewBag.FechaSeleccionada = targetFecha;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnularVenta(Guid id, string motivo)
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await _ventaService.AnularVentaAsync(id, userId, motivo ?? "Anulación solicitada por gerencia");

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
            }
            else
            {
                TempData["SuccessMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Ventas));
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
