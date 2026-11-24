using DC_BOUTIQUE.Core.Application.DTOs.Caja;
using DC_BOUTIQUE.Core.Application.Interfaces;
using DC_BOUTIQUE.Core.Application.Wrappers;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Core.Domain.Interfaces;

namespace DC_BOUTIQUE.Core.Application.Services
{
    public class CajaService : ICajaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CajaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TurnoCajaDto?> GetTurnoAbiertoAsync(Guid? usuarioId = null, CancellationToken cancellationToken = default)
        {
            var turno = await _unitOfWork.TurnosCaja.GetTurnoAbiertoAsync(usuarioId, cancellationToken);
            if (turno == null) return null;

            return await CalcularTotalesTurnoAsync(turno, cancellationToken);
        }

        public async Task<TurnoCajaDto?> GetUltimoTurnoAsync(CancellationToken cancellationToken = default)
        {
            var turno = await _unitOfWork.TurnosCaja.GetUltimoTurnoAsync(cancellationToken);
            if (turno == null) return null;

            return await CalcularTotalesTurnoAsync(turno, cancellationToken);
        }

        public async Task<TurnoCajaDto?> GetTurnoPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var turno = await _unitOfWork.TurnosCaja.GetByIdAsync(id, cancellationToken);
            if (turno == null) return null;

            return await CalcularTotalesTurnoAsync(turno, cancellationToken);
        }

        public async Task<List<TurnoCajaDto>> GetHistorialTurnosAsync(DateTime? fechaDesde = null, DateTime? fechaHasta = null, CancellationToken cancellationToken = default)
        {
            var turnos = await _unitOfWork.TurnosCaja.GetAllListWithInclude(new List<string> { "Usuario" }, cancellationToken);

            var query = turnos.AsEnumerable();
            if (fechaDesde.HasValue)
            {
                query = query.Where(t => t.FechaApertura.Date >= fechaDesde.Value.Date);
            }
            if (fechaHasta.HasValue)
            {
                query = query.Where(t => t.FechaApertura.Date <= fechaHasta.Value.Date);
            }

            var ordenados = query.OrderByDescending(t => t.FechaApertura).ToList();
            var dtos = new List<TurnoCajaDto>();
            foreach (var t in ordenados)
            {
                dtos.Add(await CalcularTotalesTurnoAsync(t, cancellationToken));
            }
            return dtos;
        }

        public async Task<Result<TurnoCajaDto>> AbrirTurnoAsync(Guid usuarioId, AbrirTurnoDto dto, CancellationToken cancellationToken = default)
        {
            var hayAbierto = await _unitOfWork.TurnosCaja.HayTurnoAbiertoAsync(cancellationToken);
            if (hayAbierto)
            {
                return Result<TurnoCajaDto>.Failure("Ya existe un turno de caja abierto en el sistema. Debe realizar el cierre antes de aperturar uno nuevo.");
            }

            if (dto.FondoInicialEfectivo < 0)
            {
                return Result<TurnoCajaDto>.Failure("El fondo inicial en efectivo no puede ser negativo.");
            }

            var localNow = Helpers.DominicanDateTime.Now;
            var codigo = $"TURNO-{localNow:yyyyMMdd}-{localNow:HHmmss}";
            var turno = new TurnoCaja
            {
                CodigoTurno = codigo,
                UsuarioId = usuarioId,
                FechaApertura = localNow,
                FondoInicialEfectivo = dto.FondoInicialEfectivo,
                Estado = EstadoCaja.Abierta,
                Observaciones = dto.Observaciones?.Trim()
            };

            await _unitOfWork.TurnosCaja.AddAsync(turno, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var turnoDto = await CalcularTotalesTurnoAsync(turno, cancellationToken);
            return Result<TurnoCajaDto>.Success(turnoDto, "Turno de caja abierto correctamente.");
        }

        public async Task<Result<TurnoCajaDto>> CerrarTurnoAsync(Guid usuarioId, CerrarTurnoDto dto, CancellationToken cancellationToken = default)
        {
            var turno = await _unitOfWork.TurnosCaja.GetByIdAsync(dto.TurnoId, cancellationToken);
            if (turno == null || turno.Estado != EstadoCaja.Abierta)
            {
                return Result<TurnoCajaDto>.Failure("El turno de caja especificado no existe o ya fue cerrado.");
            }

            var totales = await CalcularTotalesTurnoAsync(turno, cancellationToken);
            var diferencia = dto.EfectivoContadoAlCierre - totales.EfectivoEsperadoEnCaja;

            turno.FechaCierre = Helpers.DominicanDateTime.Now;
            turno.EfectivoContadoAlCierre = dto.EfectivoContadoAlCierre;
            turno.DiferenciaArqueo = diferencia;
            turno.Estado = Math.Abs(diferencia) < 0.01m ? EstadoCaja.CerradaCuadrada : EstadoCaja.CerradaConDiferencia;
            turno.Observaciones = dto.Observaciones?.Trim();

            await _unitOfWork.TurnosCaja.UpdateAsync(turno, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var cerradoDto = await CalcularTotalesTurnoAsync(turno, cancellationToken);
            return Result<TurnoCajaDto>.Success(cerradoDto, "Cierre de caja completado exitosamente.");
        }

        public async Task<Result<GastoCajaDto>> RegistrarGastoAsync(Guid usuarioId, SaveGastoCajaDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.Monto <= 0)
            {
                return Result<GastoCajaDto>.Failure("El monto del gasto o egreso debe ser mayor a RD$ 0.00.");
            }

            if (string.IsNullOrWhiteSpace(dto.Concepto))
            {
                return Result<GastoCajaDto>.Failure("El concepto o motivo del gasto es obligatorio.");
            }

            var turno = await _unitOfWork.TurnosCaja.GetByIdAsync(dto.TurnoCajaId, cancellationToken);
            if (turno == null || turno.Estado != EstadoCaja.Abierta)
            {
                return Result<GastoCajaDto>.Failure("No se pueden registrar gastos cuando la caja se encuentra cerrada.");
            }

            var gasto = new GastoCaja
            {
                TurnoCajaId = dto.TurnoCajaId,
                UsuarioId = usuarioId,
                Concepto = dto.Concepto.Trim(),
                CategoriaGasto = dto.CategoriaGasto?.Trim() ?? "General",
                Monto = dto.Monto,
                MetodoPago = dto.MetodoPago,
                ComprobanteReferencia = dto.ComprobanteReferencia?.Trim(),
                Observaciones = dto.Observaciones?.Trim()
            };

            await _unitOfWork.GastosCaja.AddAsync(gasto, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var gastoDto = new GastoCajaDto
            {
                Id = gasto.Id,
                TurnoCajaId = gasto.TurnoCajaId ?? Guid.Empty,
                UsuarioId = gasto.UsuarioId,
                Concepto = gasto.Concepto,
                CategoriaGasto = gasto.CategoriaGasto,
                Monto = gasto.Monto,
                MetodoPago = gasto.MetodoPago,
                ComprobanteReferencia = gasto.ComprobanteReferencia,
                Observaciones = gasto.Observaciones,
                FechaCreacion = gasto.FechaCreacion,
                Activo = gasto.Activo
            };

            return Result<GastoCajaDto>.Success(gastoDto, "Gasto registrado exitosamente.");
        }

        public async Task<List<GastoCajaDto>> GetGastosPorTurnoAsync(Guid turnoId, CancellationToken cancellationToken = default)
        {
            var gastos = await _unitOfWork.GastosCaja.GetPorTurnoAsync(turnoId, cancellationToken);
            return gastos.Select(g => new GastoCajaDto
            {
                Id = g.Id,
                TurnoCajaId = g.TurnoCajaId ?? Guid.Empty,
                UsuarioId = g.UsuarioId,
                NombreUsuario = g.Usuario?.NombreCompleto ?? string.Empty,
                Concepto = g.Concepto,
                CategoriaGasto = g.CategoriaGasto,
                Monto = g.Monto,
                MetodoPago = g.MetodoPago,
                ComprobanteReferencia = g.ComprobanteReferencia,
                Observaciones = g.Observaciones,
                FechaCreacion = g.FechaCreacion,
                Activo = g.Activo
            }).ToList();
        }

        private async Task<TurnoCajaDto> CalcularTotalesTurnoAsync(TurnoCaja turno, CancellationToken cancellationToken)
        {
            var ventas = await _unitOfWork.Ventas.GetPorTurnoAsync(turno.Id, cancellationToken);
            var gastos = await _unitOfWork.GastosCaja.GetPorTurnoAsync(turno.Id, cancellationToken);

            var totalVentas = ventas.Where(v => v.Estado == EstadoVenta.Completada).Sum(v => v.TotalNeto);
            var totalEfectivo = ventas
                .Where(v => v.Estado == EstadoVenta.Completada && v.Pagos != null)
                .SelectMany(v => v.Pagos ?? Enumerable.Empty<Core.Domain.Entities.PagoVenta>())
                .Where(p => p != null && p.MetodoPago == MetodoPago.Efectivo)
                .Sum(p => p.Monto);

            var totalTarjeta = ventas
                .Where(v => v.Estado == EstadoVenta.Completada && v.Pagos != null)
                .SelectMany(v => v.Pagos ?? Enumerable.Empty<Core.Domain.Entities.PagoVenta>())
                .Where(p => p != null && (p.MetodoPago == MetodoPago.TarjetaCredito || p.MetodoPago == MetodoPago.TarjetaDebito))
                .Sum(p => p.Monto);

            var totalTransferencia = ventas
                .Where(v => v.Estado == EstadoVenta.Completada && v.Pagos != null)
                .SelectMany(v => v.Pagos ?? Enumerable.Empty<Core.Domain.Entities.PagoVenta>())
                .Where(p => p != null && p.MetodoPago == MetodoPago.TransferenciaBancaria)
                .Sum(p => p.Monto);

            var totalGastos = gastos.Where(g => g.Activo).Sum(g => g.Monto);
            var gastosEfectivo = gastos.Where(g => g.Activo && g.MetodoPago == MetodoPago.Efectivo).Sum(g => g.Monto);

            string nombreUsuario = turno.Usuario?.NombreCompleto ?? string.Empty;
            if (string.IsNullOrWhiteSpace(nombreUsuario) && turno.UsuarioId != Guid.Empty)
            {
                var u = await _unitOfWork.Usuarios.GetByIdAsync(turno.UsuarioId, cancellationToken);
                nombreUsuario = u?.NombreCompleto ?? u?.NombreUsuario ?? "Cajero";
            }
            if (string.IsNullOrWhiteSpace(nombreUsuario)) nombreUsuario = "Cajero";

            return new TurnoCajaDto
            {
                Id = turno.Id,
                CodigoTurno = turno.CodigoTurno,
                UsuarioId = turno.UsuarioId,
                NombreUsuario = nombreUsuario,
                FechaApertura = turno.FechaApertura,
                FechaCierre = turno.FechaCierre,
                FondoInicialEfectivo = turno.FondoInicialEfectivo,
                TotalVentasEfectivo = totalEfectivo,
                TotalVentasTarjeta = totalTarjeta,
                TotalVentasTransferencia = totalTransferencia,
                TotalGastosEfectivo = gastosEfectivo,
                EfectivoContadoAlCierre = turno.EfectivoContadoAlCierre,
                DiferenciaArqueo = turno.DiferenciaArqueo,
                Estado = turno.Estado,
                Observaciones = turno.Observaciones
            };
        }

        public async Task LimpiarTurnosCerradosAntiguosAsync(CancellationToken cancellationToken = default)
        {
            var demoCutoff = new DateTime(2026, 11, 24, 7, 0, 0, DateTimeKind.Utc);
            var turnosAntiguos = _unitOfWork.TurnosCaja.GetAllQuery()
                .Where(t => t.Estado != EstadoCaja.Abierta && t.FechaApertura < demoCutoff)
                .ToList();

            if (!turnosAntiguos.Any()) return;

            var turnosIds = turnosAntiguos.Select(t => t.Id).ToList();

            var ventasAntiguas = _unitOfWork.Ventas.GetAllQuery()
                .Where(v => turnosIds.Contains(v.TurnoCajaId))
                .ToList();

            if (ventasAntiguas.Any())
            {
                foreach (var v in ventasAntiguas)
                {
                    await _unitOfWork.Ventas.HardDeleteAsync(v.Id, cancellationToken);
                }
            }

            var gastosAntiguos = _unitOfWork.GastosCaja.GetAllQuery()
                .Where(g => g.TurnoCajaId.HasValue && turnosIds.Contains(g.TurnoCajaId.Value))
                .ToList();

            if (gastosAntiguos.Any())
            {
                foreach (var g in gastosAntiguos)
                {
                    await _unitOfWork.GastosCaja.HardDeleteAsync(g.Id, cancellationToken);
                }
            }

            foreach (var t in turnosAntiguos)
            {
                await _unitOfWork.TurnosCaja.HardDeleteAsync(t.Id, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
