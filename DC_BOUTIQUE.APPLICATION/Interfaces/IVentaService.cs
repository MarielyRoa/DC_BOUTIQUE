using DC_BOUTIQUE.Core.Application.DTOs.Ventas;
using DC_BOUTIQUE.Core.Application.Wrappers;

namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public interface IVentaService
    {
        Task<VentaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<VentaDto>> RegistrarVentaAsync(Guid usuarioId, RegistrarVentaDto dto, CancellationToken cancellationToken = default);
        Task<Result> AnularVentaAsync(Guid ventaId, Guid usuarioId, string motivo, CancellationToken cancellationToken = default);
        Task<List<VentaDto>> GetVentasDelDiaAsync(DateTime fecha, CancellationToken cancellationToken = default);
        Task<List<VentaDto>> GetVentasPorTurnoAsync(Guid turnoId, CancellationToken cancellationToken = default);
    }
}
