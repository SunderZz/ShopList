using System.Net;
using ListeDeCourses.Api.Common;
using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Mappings;
using ListeDeCourses.Api.Models;
using ListeDeCourses.Api.Repositories;
using MongoDB.Driver;

namespace ListeDeCourses.Api.Services;

public class PlatService : BaseService<PlatReadDto, PlatCreateDto, PlatUpdateDto, Plat>
{
    private readonly PlatRepository _plats;
    private readonly ListeService _listes;

    public PlatService(PlatRepository repository, ListeService listes, IHttpContextAccessor httpContextAccessor)
        : base(repository, httpContextAccessor)
    {
        _plats = repository;
        _listes = listes;
    }

    protected override PlatReadDto MapToReadDto(Plat entity) => entity.ToReadDto();
    protected override Plat MapToEntity(PlatCreateDto dto) => dto.ToModel();
    protected override void ApplyUpdate(Plat entity, PlatUpdateDto dto) => entity.Apply(dto);

    public override async Task<PlatReadDto> CreateAsync(PlatCreateDto dto, CancellationToken ct = default)
    {
        EnsureAuthenticated();

        var entity = dto.ToModel();
        ApplyNormalizedName(entity, dto.Name);
        await EnsureNameAvailableAsync(entity.Name, entity.NameKey!, excludeId: null, ct);

        try
        {
            await _plats.CreateAsync(entity, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicateNameException(ex);
        }

        return entity.ToReadDto();
    }

    public override async Task<PlatReadDto?> UpdateAsync(string id, PlatUpdateDto dto, CancellationToken ct = default)
    {
        EnsureAuthenticated();

        var existing = await _plats.GetByIdAsync(id, ct);
        if (existing is null) return default;

        if (dto.Name is not null)
        {
            var normalizedName = NameNormalizer.NormalizeDisplayName(dto.Name);
            var nameKey = NameNormalizer.ToKey(normalizedName);
            var currentKey = NameNormalizer.ToKey(existing.Name);

            if (!string.Equals(currentKey, nameKey, StringComparison.Ordinal))
            {
                await EnsureNameAvailableAsync(normalizedName, nameKey, id, ct);
            }

            existing.Name = normalizedName;
            existing.NameKey = nameKey;
        }

        if (dto.Ingredients is not null)
        {
            existing.Ingredients = dto.Ingredients.Select(i => new PlatIngredient
            {
                IngredientId = i.IngredientId,
                Quantity = i.Quantity,
                Unit = i.Unit
            }).ToList();
        }

        try
        {
            await _plats.UpdateAsync(id, existing, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicateNameException(ex);
        }

        return existing.ToReadDto();
    }

    public override async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        var ok = await _repository.DeleteAsync(id, ct);
        if (!ok) return false;

        await _listes.CascadeRemoveDishAsync(id, ct);
        return true;
    }

    public async Task CascadeRemoveIngredientAsync(string ingredientId, CancellationToken ct)
    {
        var all = await _repository.GetAllAsync(ct);
        foreach (var p in all.Where(p => p.Ingredients.Any(i => i.IngredientId == ingredientId)))
        {
            p.Ingredients.RemoveAll(i => i.IngredientId == ingredientId);
            await _repository.UpdateAsync(p.Id, p, ct);
        }
    }

    private async Task EnsureNameAvailableAsync(
        string normalizedName,
        string nameKey,
        string? excludeId,
        CancellationToken ct)
    {
        var namePattern = NameNormalizer.ToExactFlexibleWhitespacePattern(normalizedName);
        var duplicate = await _plats.FindByNameKeyOrNamePatternAsync(nameKey, namePattern, excludeId, ct);
        if (duplicate is not null)
        {
            throw DuplicateNameException();
        }
    }

    private static void ApplyNormalizedName(Plat entity, string name)
    {
        entity.Name = NameNormalizer.NormalizeDisplayName(name);
        entity.NameKey = NameNormalizer.ToKey(entity.Name);
    }

    private static DomainException DuplicateNameException(Exception? inner = null)
    {
        const string message = "Un plat avec ce nom existe déjà.";
        return inner is null
            ? new DomainException(message, code: "DISH_DUPLICATE", httpStatus: HttpStatusCode.Conflict)
            : new DomainException(message, inner, code: "DISH_DUPLICATE", httpStatus: HttpStatusCode.Conflict);
    }
}
