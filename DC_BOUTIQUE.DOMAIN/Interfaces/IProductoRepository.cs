using DC_BOUTIQUE.Core.Domain.Entities;

namespace DC_BOUTIQUE.Core.Domain.Interfaces
{
    public interface IProductoRepository : IGenericRepository<Producto>
    {
        Task<Producto?> GetDetalleCompletoAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Producto>> GetPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);
        Task<bool> ExisteCodigoModeloAsync(string codigoModelo, Guid? excluirId = null, CancellationToken cancellationToken = default);
    }
}
