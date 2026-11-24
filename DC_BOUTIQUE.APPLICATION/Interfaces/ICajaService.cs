using DC_BOUTIQUE.Core.Application.DTOs.Caja;
using DC_BOUTIQUE.Core.Application.Wrappers;

namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public interface ICajaService
    {
        Task<TurnoCajaDto?> GetTurnoAbiertoAsync(Guid? usuarioId = null, CancellationToken cancellationToken = default);
        Task<TurnoCajaDto?> GetUltimoTurnoAsync(CancellationToken cancellationToken = default);
        Task<TurnoCajaDto?> GetTurnoPorIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<TurnoCajaDto>> GetHistorialTurnosAsync(DateTime? fechaDesde = null, DateTime? fechaHasta = null, CancellationToken cancellationToken = default);
        Task<Result<TurnoCajaDto>> AbrirTurnoAsync(Guid usuarioId, AbrirTurnoDto dto, CancellationToken cancellationToken = default);
        Task<Result<TurnoCajaDto>> CerrarTurnoAsync(Guid usuarioId, CerrarTurnoDto dto, CancellationToken cancellationToken = default);
        Task<Result<GastoCajaDto>> RegistrarGastoAsync(Guid usuarioId, SaveGastoCajaDto dto, CancellationToken cancellationToken = default);
        Task<List<GastoCajaDto>> GetGastosPorTurnoAsync(Guid turnoId, CancellationToken cancellationToken = default);
    }
}
