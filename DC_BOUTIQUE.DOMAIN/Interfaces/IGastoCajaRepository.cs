using DC_BOUTIQUE.Core.Domain.Entities;

namespace DC_BOUTIQUE.Core.Domain.Interfaces
{
    public interface IGastoCajaRepository : IGenericRepository<GastoCaja>
    {
        Task<List<GastoCaja>> GetPorTurnoAsync(Guid turnoCajaId, CancellationToken cancellationToken = default);
        Task<decimal> GetTotalGastosPorTurnoAsync(Guid turnoCajaId, CancellationToken cancellationToken = default);
    }
}
