using DC_BOUTIQUE.Core.Application.DTOs.Common;
using DC_BOUTIQUE.Core.Application.Wrappers;

namespace DC_BOUTIQUE.Core.Application.Interfaces
{
    public interface IGenericService<TDto> where TDto : class
    {
        Task<TDto?> AddAsync(TDto dto);
        Task<TDto?> UpdateAsync(TDto dto, Guid id);
        Task<bool> DeleteAsync(Guid id);
        Task<List<TDto>> GetAllAsync();
        Task<List<TDto>> GetAllWithIncludeAsync(List<string> properties);
        Task<TDto?> GetByIdAsync(Guid id);
    }

    public interface IGenericService<TDto, TSaveDto> 
        where TDto : BasicDto 
        where TSaveDto : class
    {
        Task<List<TDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<TDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<TDto>> CreateAsync(TSaveDto dto, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(Guid id, TSaveDto dto, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
