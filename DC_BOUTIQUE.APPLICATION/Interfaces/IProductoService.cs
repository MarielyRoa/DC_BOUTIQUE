using DC_BOUTIQUE.Core.Application.DTOs.Productos;
using DC_BOUTIQUE.Core.Application.Wrappers;

namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public interface IProductoService : IGenericService<ProductoDto, SaveProductoDto>
    {
        Task<List<ProductoDto>> GetPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);
        Task<ProductoVarianteDto?> GetVarianteBySkuAsync(string sku, CancellationToken cancellationToken = default);
        Task<ProductoVarianteDto?> GetVarianteByCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default);
        Task<List<ProductoVarianteDto>> GetVariantesConStockBajoAsync(CancellationToken cancellationToken = default);
        Task<Result> AgregarVarianteAsync(SaveProductoVarianteDto dto, CancellationToken cancellationToken = default);
        Task<Result> ActualizarVarianteAsync(Guid id, SaveProductoVarianteDto dto, CancellationToken cancellationToken = default);
        Task<Result> EliminarVarianteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
