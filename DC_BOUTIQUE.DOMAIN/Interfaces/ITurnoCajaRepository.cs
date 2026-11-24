using DC_BOUTIQUE.Core.Domain.Entities;

namespace DC_BOUTIQUE.Core.Domain.Interfaces
{
    public interface ITurnoCajaRepository : IGenericRepository<TurnoCaja>
    {
        Task<TurnoCaja?> GetTurnoAbiertoAsync(Guid? usuarioId = null, CancellationToken cancellationToken = default);
        Task<TurnoCaja?> GetUltimoTurnoAsync(CancellationToken cancellationToken = default);
        Task<bool> HayTurnoAbiertoAsync(CancellationToken cancellationToken = default);
    }
}
