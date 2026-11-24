using DC_BOUTIQUE.Core.Application.DTOs.Ventas;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.Wrappers;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;

namespace DC_BOUTIQUE.Core.Application.Services
{
    public class VentaService : IVentaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public VentaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<VentaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var v = await _unitOfWork.Ventas.GetDetalleCompletoAsync(id, cancellationToken);
            return v == null ? null : MapToDto(v);
        }

        public async Task<List<VentaDto>> GetVentasDelDiaAsync(DateTime fecha, CancellationToken cancellationToken = default)
        {
            var ventas = await _unitOfWork.Ventas.GetVentasDelDiaAsync(fecha, cancellationToken);
            return ventas.Select(MapToDto).ToList();
        }

        public async Task<List<VentaDto>> GetVentasPorTurnoAsync(Guid turnoId, CancellationToken cancellationToken = default)
        {
            var ventas = await _unitOfWork.Ventas.GetPorTurnoAsync(turnoId, cancellationToken);
            return ventas.Select(MapToDto).ToList();
        }

        public async Task<Result<VentaDto>> RegistrarVentaAsync(Guid usuarioId, RegistrarVentaDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.Items == null || dto.Items.Count == 0)
            {
                return Result<VentaDto>.Failure("No hay prendas en el carrito para procesar la venta.");
            }

            // 1. Validar que la caja esté abierta
            var turno = await _unitOfWork.TurnosCaja.GetByIdAsync(dto.TurnoCajaId, cancellationToken);
            if (turno == null || turno.Estado != EstadoCaja.Abierta)
            {
                return Result<VentaDto>.Failure("No se pueden registrar ventas sin un turno de caja abierto.");
            }

            // Validar stock antes de abrir transacción
            foreach (var item in dto.Items)
            {
                var variante = await _unitOfWork.ProductoVariantes.GetByIdAsync(item.ProductoVarianteId, cancellationToken);
                if (variante == null || !variante.Activo)
                {
                    return Result<VentaDto>.Failure("Una de las prendas seleccionadas no existe o está inactiva en el catálogo.");
                }

                if (variante.StockActual < item.Cantidad)
                {
                    return Result<VentaDto>.Failure($"Stock insuficiente para la prenda SKU '{variante.Sku}'. Stock disponible: {variante.StockActual}, solicitado: {item.Cantidad}.");
                }
            }

            // 2. Iniciar transacción atómica (Venta + Descuento Stock + Kardex)
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var localNow = Helpers.DominicanDateTime.Now;
                var numeroFactura = $"VENTA-{localNow:yyyyMMdd}-{localNow:HHmmss}";
                var venta = new Venta
                {
                    NumeroFactura = numeroFactura,
                    TipoComprobante = dto.TipoComprobante,
                    ClienteId = dto.ClienteId,
                    UsuarioId = usuarioId,
                    TurnoCajaId = dto.TurnoCajaId,
                    FechaVenta = localNow,
                    Estado = EstadoVenta.Completada,
                    Observaciones = dto.Observaciones?.Trim()
                };

                decimal subtotalAcumulado = 0m;
                decimal descuentoAcumulado = 0m;

                // 3. Procesar cada prenda del carrito
                foreach (var item in dto.Items)
                {
                    var variante = await _unitOfWork.ProductoVariantes.GetByIdAsync(item.ProductoVarianteId, cancellationToken);
                    if (variante == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<VentaDto>.Failure("Error al consultar la prenda.");
                    }

                    var producto = await _unitOfWork.Productos.GetByIdAsync(variante.ProductoId, cancellationToken);
                    if (producto == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<VentaDto>.Failure($"El producto de la prenda SKU '{variante.Sku}' no fue encontrado.");
                    }

                    var precioUnitario = (producto.PrecioOferta.HasValue && producto.PrecioOferta.Value > 0)
                        ? producto.PrecioOferta.Value
                        : producto.PrecioVentaBase;

                    var costoUnitario = producto.CostoBaseReferencia;
                    var baseLinea = precioUnitario * item.Cantidad;
                    var montoDescuento = baseLinea * (item.PorcentajeDescuento / 100m);
                    var netoLinea = baseLinea - montoDescuento;

                    subtotalAcumulado += baseLinea;
                    descuentoAcumulado += montoDescuento;

                    var detalle = new DetalleVenta
                    {
                        VentaId = venta.Id,
                        ProductoVarianteId = variante.Id,
                        SkuSnapshot = variante.Sku,
                        NombreProductoSnapshot = producto.Nombre,
                        TallaSnapshot = variante.Talla,
                        ColorSnapshot = variante.Color,
                        Cantidad = item.Cantidad,
                        CostoUnitario = costoUnitario,
                        PrecioUnitario = precioUnitario,
                        PorcentajeDescuento = item.PorcentajeDescuento,
                        MontoDescuento = montoDescuento,
                        MontoImpuesto = 0m,
                        SubtotalLinea = netoLinea
                    };
                    venta.Detalles.Add(detalle);

                    // 4. Descontar Stock de la variante
                    var stockAnterior = variante.StockActual;
                    variante.StockActual -= item.Cantidad;
                    variante.ConcurrencyToken = Guid.NewGuid();
                    await _unitOfWork.ProductoVariantes.UpdateAsync(variante, cancellationToken);

                    // 5. Registrar Kardex
                    await _unitOfWork.MovimientosInventario.AddAsync(new MovimientoInventario
                    {
                        ProductoVarianteId = variante.Id,
                        UsuarioId = usuarioId,
                        TipoMovimiento = TipoMovimientoInventario.SalidaPorVenta,
                        Cantidad = item.Cantidad,
                        StockAnterior = stockAnterior,
                        StockPosterior = variante.StockActual,
                        CostoUnitarioReferencia = costoUnitario,
                        DocumentoReferencia = numeroFactura,
                        Motivo = $"Venta POS #{numeroFactura}"
                    }, cancellationToken);
                }

                venta.Subtotal = subtotalAcumulado;
                venta.DescuentoTotal = descuentoAcumulado;
                venta.ImpuestoTotal = 0m;
                venta.TotalNeto = subtotalAcumulado - descuentoAcumulado;

                // 6. Validar pagos
                var totalPagado = dto.Pagos.Sum(p => p.Monto);
                if (totalPagado < venta.TotalNeto - 0.05m)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<VentaDto>.Failure($"Monto pagado insuficiente. Total a cobrar: RD${venta.TotalNeto:N2}, ingresado: RD${totalPagado:N2}.");
                }

                foreach (var p in dto.Pagos)
                {
                    venta.Pagos.Add(new PagoVenta
                    {
                        VentaId = venta.Id,
                        MetodoPago = p.MetodoPago,
                        Monto = p.Monto,
                        ReferenciaTransaccion = p.Referencia?.Trim()
                    });
                }

                await _unitOfWork.Ventas.AddAsync(venta, cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var ventaCompleta = await _unitOfWork.Ventas.GetDetalleCompletoAsync(venta.Id, cancellationToken);
                return Result<VentaDto>.Success(MapToDto(ventaCompleta ?? venta), $"Venta #{venta.NumeroFactura} procesada exitosamente.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<VentaDto>.Failure($"Error al procesar la venta: {ex.Message}");
            }
        }

        public async Task<Result> AnularVentaAsync(Guid ventaId, Guid usuarioId, string motivo, CancellationToken cancellationToken = default)
        {
            var venta = await _unitOfWork.Ventas.GetDetalleCompletoAsync(ventaId, cancellationToken);
            if (venta == null || venta.Estado == EstadoVenta.Anulada)
            {
                return Result.Failure("La venta no existe o ya se encuentra anulada.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                venta.Estado = EstadoVenta.Anulada;
                venta.Observaciones = $"ANULADA: {motivo.Trim()} (por usuario {usuarioId})";
                await _unitOfWork.Ventas.UpdateAsync(venta, cancellationToken);

                // Reingresar stock de cada prenda devuelta
                foreach (var d in venta.Detalles)
                {
                    var variante = await _unitOfWork.ProductoVariantes.GetByIdAsync(d.ProductoVarianteId, cancellationToken);
                    if (variante != null)
                    {
                        var stockAnterior = variante.StockActual;
                        variante.StockActual += d.Cantidad;
                        variante.ConcurrencyToken = Guid.NewGuid();
                        await _unitOfWork.ProductoVariantes.UpdateAsync(variante, cancellationToken);

                        await _unitOfWork.MovimientosInventario.AddAsync(new MovimientoInventario
                        {
                            ProductoVarianteId = variante.Id,
                            UsuarioId = usuarioId,
                            TipoMovimiento = TipoMovimientoInventario.AjusteInventario,
                            Cantidad = d.Cantidad,
                            StockAnterior = stockAnterior,
                            StockPosterior = variante.StockActual,
                            CostoUnitarioReferencia = d.CostoUnitario,
                            DocumentoReferencia = venta.NumeroFactura,
                            Motivo = $"Reingreso por anulación de venta #{venta.NumeroFactura}"
                        }, cancellationToken);
                    }
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result.Success($"Venta #{venta.NumeroFactura} anulada correctamente y stock reingresado.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure($"Error al anular la venta: {ex.Message}");
            }
        }

        private static VentaDto MapToDto(Venta v)
        {
            return new VentaDto
            {
                Id = v.Id,
                NumeroFactura = v.NumeroFactura,
                TipoComprobante = v.TipoComprobante,
                ClienteId = v.ClienteId,
                NombreCliente = v.Cliente != null ? $"{v.Cliente.Nombres} {v.Cliente.Apellidos}".Trim() : "Consumidor Final",
                UsuarioId = v.UsuarioId,
                NombreUsuario = v.Usuario?.NombreCompleto ?? "Cajero",
                TurnoCajaId = v.TurnoCajaId,
                FechaVenta = v.FechaVenta,
                Subtotal = v.Subtotal,
                DescuentoTotal = v.DescuentoTotal,
                ImpuestoTotal = v.ImpuestoTotal,
                TotalNeto = v.TotalNeto,
                Estado = v.Estado,
                Observaciones = v.Observaciones,
                Detalles = v.Detalles.Select(d => new DetalleVentaDto
                {
                    Id = d.Id,
                    VentaId = d.VentaId,
                    ProductoVarianteId = d.ProductoVarianteId,
                    SkuSnapshot = d.SkuSnapshot,
                    NombreProductoSnapshot = d.NombreProductoSnapshot,
                    TallaSnapshot = d.TallaSnapshot,
                    ColorSnapshot = d.ColorSnapshot,
                    Cantidad = d.Cantidad,
                    CostoUnitario = d.CostoUnitario,
                    PrecioUnitario = d.PrecioUnitario,
                    PorcentajeDescuento = d.PorcentajeDescuento,
                    MontoDescuento = d.MontoDescuento,
                    MontoImpuesto = d.MontoImpuesto,
                    SubtotalLinea = d.SubtotalLinea
                }).ToList(),
                Pagos = v.Pagos.Select(p => new PagoVentaDto
                {
                    Id = p.Id,
                    VentaId = p.VentaId,
                    MetodoPago = p.MetodoPago,
                    Monto = p.Monto,
                    ReferenciaTransaccion = p.ReferenciaTransaccion
                }).ToList()
            };
        }
    }
}
