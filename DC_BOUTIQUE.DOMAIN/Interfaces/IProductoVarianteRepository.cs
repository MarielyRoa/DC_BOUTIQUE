using DC_BOUTIQUE.Core.Domain.Entities;

namespace DC_BOUTIQUE.Core.Domain.Interfaces
{
    public interface IProductoVarianteRepository : IGenericRepository<ProductoVariante>
    {
        Task<ProductoVariante?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
        Task<ProductoVariante?> GetByCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default);
        Task<List<ProductoVariante>> GetPorProductoIdAsync(Guid productoId, CancellationToken cancellationToken = default);
        Task<List<ProductoVariante>> GetConStockBajoAsync(CancellationToken cancellationToken = default);
        Task<bool> ExisteSkuAsync(string sku, Guid? excluirId = null, CancellationToken cancellationToken = default);
    }
}
