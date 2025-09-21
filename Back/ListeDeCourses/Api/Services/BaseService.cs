using ListeDeCourses.Api.Repositories;

namespace ListeDeCourses.Api.Services;

public abstract class BaseService<TReadDto, TCreateDto, TUpdateDto, TEntity> 
    : IBaseService<TReadDto, TCreateDto, TUpdateDto>
    where TEntity : class, new()
{
    protected readonly BaseRepository<TEntity> _repository;

    protected BaseService(BaseRepository<TEntity> repository)
    {
        _repository = repository;
    }

    public virtual async Task<IEnumerable<TReadDto>> GetAllAsync(CancellationToken ct = default) =>
        (await _repository.GetAllAsync(ct)).Select(MapToReadDto);

    public virtual async Task<TReadDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        return entity is null ? default : MapToReadDto(entity);
    }

    public virtual async Task<TReadDto> CreateAsync(TCreateDto dto, CancellationToken ct = default)
    {
        var entity = MapToEntity(dto);
        await _repository.CreateAsync(entity, ct);
        return MapToReadDto(entity);
    }

    public virtual async Task<TReadDto?> UpdateAsync(string id, TUpdateDto dto, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return default;

        ApplyUpdate(existing, dto);
        await _repository.UpdateAsync(id, existing, ct);

        return MapToReadDto(existing);
    }

    public virtual async Task<bool> DeleteAsync(string id, CancellationToken ct = default) =>
        await _repository.DeleteAsync(id, ct);

    protected abstract TReadDto MapToReadDto(TEntity entity);
    protected abstract TEntity MapToEntity(TCreateDto dto);
    protected abstract void ApplyUpdate(TEntity entity, TUpdateDto dto);
}
