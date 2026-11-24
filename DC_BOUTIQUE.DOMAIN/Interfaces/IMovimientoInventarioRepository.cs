using DC_BOUTIQUE.Core.Domain.Entities;

namespace DC_BOUTIQUE.Core.Domain.Interfaces
{
    public interface IMovimientoInventarioRepository : IGenericRepository<MovimientoInventario>
    {
        Task<List<MovimientoInventario>> GetPorVarianteAsync(Guid productoVarianteId, CancellationToken cancellationToken = default);
        Task<List<MovimientoInventario>> GetHistorialRecienteAsync(int cantidad = 50, CancellationToken cancellationToken = default);
    }
}
