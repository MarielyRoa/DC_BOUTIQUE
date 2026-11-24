using DC_BOUTIQUE.Core.Application.ViewModels.Admin;
using DC_BOUTIQUE.Core.Domain.Interfaces;
using DC_BOUTIQUE.WEBAPP.Middlewares;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.WEBAPP.Controllers
{
    [AuthorizeAdmin]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.UtcNow.Date;
            var primerDiaMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            // Ventas de hoy y del mes
            var ventasHoy = await _unitOfWork.Ventas.GetAllQuery()
                .Where(v => v.FechaVenta >= hoy && v.Activo)
                .ToListAsync();

            decimal totalVentasHoy = ventasHoy.Sum(v => v.TotalNeto);
            int cantidadVentasHoy = ventasHoy.Count;

            decimal ventasMes = await _unitOfWork.Ventas.GetAllQuery()
                .Where(v => v.FechaVenta >= primerDiaMes && v.Activo)
                .SumAsync(v => v.TotalNeto);

            // Turno de caja actual
            var ultimoTurno = await _unitOfWork.TurnosCaja.GetAllQuery()
                .Include(t => t.Usuario)
                .OrderByDescending(t => t.FechaApertura)
                .FirstOrDefaultAsync();

            bool hayTurnoAbierto = ultimoTurno != null && ultimoTurno.Estado == Core.Domain.Common.Enums.EstadoCaja.Abierta;

            // Alertas de stock bajo en prendas
            var stockBajo = await _unitOfWork.ProductoVariantes.GetAllQuery()
                .Include(v => v.Producto)
                .Where(v => v.Activo && v.Producto != null && v.Producto.Activo && v.StockActual <= v.StockMinimoAlerta)
                .OrderBy(v => v.StockActual)
                .Take(10)
                .ToListAsync();

            // Últimas ventas para el dashboard
            var ultimasVentas = await _unitOfWork.Ventas.GetAllQuery()
                .Include(v => v.Cliente)
                .Include(v => v.Pagos)
                .Where(v => v.Activo)
                .OrderByDescending(v => v.FechaVenta)
                .Take(6)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                VentasHoy = totalVentasHoy,
                CantidadVentasHoy = cantidadVentasHoy,
                VentasMes = ventasMes,
                ProductosStockBajo = stockBajo.Count,
                HayTurnoCajaAbierto = hayTurnoAbierto,
                CajeroActual = ultimoTurno?.Usuario?.NombreCompleto,
                EfectivoEnCajaActual = ultimoTurno != null ? ultimoTurno.FondoInicialEfectivo : 0m,
                AlertasStock = stockBajo.Select(v => new ProductoBajoStockItemViewModel
                {
                    NombreProducto = v.Producto != null ? v.Producto.Nombre : "Prenda",
                    Sku = v.Sku,
                    Talla = v.Talla,
                    Color = v.Color,
                    StockActual = v.StockActual,
                    StockMinimo = v.StockMinimoAlerta
                }).ToList(),
                UltimasVentas = ultimasVentas.Select(v => new VentaRecienteItemViewModel
                {
                    NumeroFactura = v.NumeroFactura,
                    Fecha = v.FechaVenta,
                    Cliente = v.Cliente != null ? $"{v.Cliente.Nombres} {v.Cliente.Apellidos}".Trim() : "Consumidor Final",
                    Total = v.TotalNeto,
                    MetodosPago = string.Join(", ", v.Pagos.Select(p => p.MetodoPago.ToString()))
                }).ToList()
            };

            return View(model);
        }
    }
}
