using DC_BOUTIQUE.Core.Application.DTOs.Inventario;
using DC_BOUTIQUE.Core.Application.Wrappers;

namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public interface IInventarioService
    {
        Task<List<MovimientoInventarioDto>> GetHistorialRecienteAsync(int cantidad = 50, CancellationToken cancellationToken = default);
        Task<List<MovimientoInventarioDto>> GetPorVarianteAsync(Guid productoVarianteId, CancellationToken cancellationToken = default);
        Task<Result> RegistrarEntradaRapidaAsync(Guid usuarioId, EntradaMercanciaRapidaDto dto, CancellationToken cancellationToken = default);
        Task<Result> RegistrarAjusteAsync(Guid usuarioId, AjusteInventarioDto dto, CancellationToken cancellationToken = default);
        Task<Result> RegistrarMermaAsync(Guid usuarioId, Guid productoVarianteId, int cantidad, string motivo, CancellationToken cancellationToken = default);
    }
}
