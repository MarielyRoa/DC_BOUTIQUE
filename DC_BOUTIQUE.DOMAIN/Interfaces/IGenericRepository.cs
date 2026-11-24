using DC_BOUTIQUE.Core.Domain.Common;

namespace DC_BOUTIQUE.Core.Domain.Interfaces
{
    public interface IGenericRepository<TEntity> where TEntity : BasicEntity
    {
        Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<TEntity>> GetAllListAsync(CancellationToken cancellationToken = default);
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<TEntity?> UpdateAsync(Guid id, TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        IQueryable<TEntity> GetAllQuery();
        IQueryable<TEntity> GetAllQueryWithInclude(List<string> properties);
        Task<List<TEntity>> GetAllListWithInclude(List<string> properties, CancellationToken cancellationToken = default);
    }
}
