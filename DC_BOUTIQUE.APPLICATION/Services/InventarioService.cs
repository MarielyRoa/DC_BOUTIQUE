using DC_BOUTIQUE.Core.Application.DTOs.Inventario;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.Wrappers;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;

namespace DC_BOUTIQUE.Core.Application.Services
{
    public class InventarioService : IInventarioService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InventarioService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<MovimientoInventarioDto>> GetHistorialRecienteAsync(int cantidad = 50, CancellationToken cancellationToken = default)
        {
            var movimientos = await _unitOfWork.MovimientosInventario.GetHistorialRecienteAsync(cantidad, cancellationToken);
            return movimientos.Select(MapToDto).ToList();
        }

        public async Task<List<MovimientoInventarioDto>> GetPorVarianteAsync(Guid productoVarianteId, CancellationToken cancellationToken = default)
        {
            var movimientos = await _unitOfWork.MovimientosInventario.GetPorVarianteAsync(productoVarianteId, cancellationToken);
            return movimientos.Select(MapToDto).ToList();
        }

        public async Task<Result> RegistrarEntradaRapidaAsync(Guid usuarioId, EntradaMercanciaRapidaDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.ProductoVarianteId == Guid.Empty)
            {
                return Result.Failure("Debe seleccionar una prenda y talla para dar entrada.");
            }

            if (dto.Cantidad <= 0)
            {
                return Result.Failure("La cantidad a ingresar debe ser mayor que cero.");
            }

            var variante = await _unitOfWork.ProductoVariantes.GetByIdAsync(dto.ProductoVarianteId, cancellationToken);
            if (variante == null || !variante.Activo)
            {
                return Result.Failure("La prenda o variante seleccionada no existe o está inactiva.");
            }

            var producto = await _unitOfWork.Productos.GetByIdAsync(variante.ProductoId, cancellationToken);
            var costo = producto?.CostoBaseReferencia ?? 0m;

            var stockAnterior = variante.StockActual;
            variante.StockActual += dto.Cantidad;
            variante.ConcurrencyToken = Guid.NewGuid();

            await _unitOfWork.ProductoVariantes.UpdateAsync(variante, cancellationToken);

            await _unitOfWork.MovimientosInventario.AddAsync(new MovimientoInventario
            {
                ProductoVarianteId = variante.Id,
                UsuarioId = usuarioId,
                TipoMovimiento = TipoMovimientoInventario.EntradaStock,
                Cantidad = dto.Cantidad,
                StockAnterior = stockAnterior,
                StockPosterior = variante.StockActual,
                CostoUnitarioReferencia = costo,
                DocumentoReferencia = dto.DocumentoReferencia?.Trim(),
                Motivo = string.IsNullOrWhiteSpace(dto.Motivo) ? "Entrada de mercancía / Reabastecimiento" : dto.Motivo.Trim()
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success($"Se registraron {dto.Cantidad} prenda(s) al inventario correctamente.");
        }

        public async Task<Result> RegistrarAjusteAsync(Guid usuarioId, AjusteInventarioDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.ProductoVarianteId == Guid.Empty)
            {
                return Result.Failure("Debe seleccionar una prenda para ajustar su stock.");
            }

            var variante = await _unitOfWork.ProductoVariantes.GetByIdAsync(dto.ProductoVarianteId, cancellationToken);
            if (variante == null || !variante.Activo)
            {
                return Result.Failure("La prenda o variante seleccionada no existe o está inactiva.");
            }

            if (dto.StockRealContado < 0)
            {
                return Result.Failure("El stock real contado no puede ser un número negativo.");
            }

            var producto = await _unitOfWork.Productos.GetByIdAsync(variante.ProductoId, cancellationToken);
            var costo = producto?.CostoBaseReferencia ?? 0m;

            var stockAnterior = variante.StockActual;
            var diferencia = dto.StockRealContado - stockAnterior;
            variante.StockActual = dto.StockRealContado;
            variante.ConcurrencyToken = Guid.NewGuid();

            await _unitOfWork.ProductoVariantes.UpdateAsync(variante, cancellationToken);

            await _unitOfWork.MovimientosInventario.AddAsync(new MovimientoInventario
            {
                ProductoVarianteId = variante.Id,
                UsuarioId = usuarioId,
                TipoMovimiento = TipoMovimientoInventario.AjusteInventario,
                Cantidad = Math.Abs(diferencia),
                StockAnterior = stockAnterior,
                StockPosterior = variante.StockActual,
                CostoUnitarioReferencia = costo,
                Motivo = $"Ajuste de inventario: {(string.IsNullOrWhiteSpace(dto.Motivo) ? "Conteo físico" : dto.Motivo.Trim())} (Diferencia: {diferencia:+#;-#;0})"
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success($"Ajuste de inventario aplicado. Nuevo stock: {dto.StockRealContado} unidades.");
        }

        public async Task<Result> RegistrarMermaAsync(Guid usuarioId, Guid productoVarianteId, int cantidad, string motivo, CancellationToken cancellationToken = default)
        {
            if (cantidad <= 0)
            {
                return Result.Failure("La cantidad de merma debe ser mayor a cero.");
            }

            var variante = await _unitOfWork.ProductoVariantes.GetByIdAsync(productoVarianteId, cancellationToken);
            if (variante == null || !variante.Activo)
            {
                return Result.Failure("La variante seleccionada no existe o está inactiva.");
            }

            if (variante.StockActual < cantidad)
            {
                return Result.Failure($"No se puede registrar merma mayor al stock disponible ({variante.StockActual} unidades).");
            }

            var producto = await _unitOfWork.Productos.GetByIdAsync(variante.ProductoId, cancellationToken);
            var costo = producto?.CostoBaseReferencia ?? 0m;

            var stockAnterior = variante.StockActual;
            variante.StockActual -= cantidad;
            variante.ConcurrencyToken = Guid.NewGuid();

            await _unitOfWork.ProductoVariantes.UpdateAsync(variante, cancellationToken);

            await _unitOfWork.MovimientosInventario.AddAsync(new MovimientoInventario
            {
                ProductoVarianteId = variante.Id,
                UsuarioId = usuarioId,
                TipoMovimiento = TipoMovimientoInventario.SalidaPorMermaODanio,
                Cantidad = cantidad,
                StockAnterior = stockAnterior,
                StockPosterior = variante.StockActual,
                CostoUnitarioReferencia = costo,
                Motivo = $"Merma / Daño: {motivo.Trim()}"
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success("Salida por merma registrada exitosamente.");
        }

        private static MovimientoInventarioDto MapToDto(MovimientoInventario m)
        {
            return new MovimientoInventarioDto
            {
                Id = m.Id,
                ProductoVarianteId = m.ProductoVarianteId,
                UsuarioId = m.UsuarioId,
                TipoMovimiento = m.TipoMovimiento,
                Cantidad = m.Cantidad,
                StockAnterior = m.StockAnterior,
                StockPosterior = m.StockPosterior,
                CostoUnitarioReferencia = m.CostoUnitarioReferencia,
                DocumentoReferencia = m.DocumentoReferencia,
                Motivo = m.Motivo,
                FechaCreacion = m.FechaCreacion,
                NombreProducto = m.ProductoVariante?.Producto?.Nombre ?? "Prenda no disponible",
                Sku = m.ProductoVariante?.Sku ?? "N/A",
                Talla = m.ProductoVariante?.Talla ?? string.Empty,
                Color = m.ProductoVariante?.Color ?? string.Empty,
                NombreUsuario = m.Usuario?.NombreUsuario ?? "Sistema"
            };
        }
    }
}
