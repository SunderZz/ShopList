using System.Net;
using ListeDeCourses.Api.Common;
using ListeDeCourses.Api.Repositories;

namespace ListeDeCourses.Api.Services;

public abstract class BaseService<TReadDto, TCreateDto, TUpdateDto, TEntity> 
    : IBaseService<TReadDto, TCreateDto, TUpdateDto>
    where TEntity : class, new()
{
    protected readonly BaseRepository<TEntity> _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    protected BaseService(BaseRepository<TEntity> repository, IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }

    protected void EnsureAuthenticated()
    {
        if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated != true)
        {
            throw new DomainException(
                "Utilisateur non authentifié.",
                code: "AUTH_REQUIRED",
                httpStatus: HttpStatusCode.Unauthorized);
        }
    }

    public virtual async Task<IEnumerable<TReadDto>> GetAllAsync(CancellationToken ct = default)
    {
        EnsureAuthenticated();
        return (await _repository.GetAllAsync(ct)).Select(MapToReadDto);
    }

    public virtual async Task<TReadDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        var entity = await _repository.GetByIdAsync(id, ct);
        return entity is null ? default : MapToReadDto(entity);
    }

    public virtual async Task<TReadDto> CreateAsync(TCreateDto dto, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        var entity = MapToEntity(dto);
        await _repository.CreateAsync(entity, ct);
        return MapToReadDto(entity);
    }

    public virtual async Task<TReadDto?> UpdateAsync(string id, TUpdateDto dto, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return default;

        ApplyUpdate(existing, dto);
        await _repository.UpdateAsync(id, existing, ct);

        return MapToReadDto(existing);
    }

    public virtual async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        return await _repository.DeleteAsync(id, ct);
    }

    protected abstract TReadDto MapToReadDto(TEntity entity);
    protected abstract TEntity MapToEntity(TCreateDto dto);
    protected abstract void ApplyUpdate(TEntity entity, TUpdateDto dto);
}
