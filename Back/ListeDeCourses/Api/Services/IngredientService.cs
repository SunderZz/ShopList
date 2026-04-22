using System.Net;
using ListeDeCourses.Api.Common;
using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Mappings;
using ListeDeCourses.Api.Models;
using ListeDeCourses.Api.Repositories;
using MongoDB.Driver;

namespace ListeDeCourses.Api.Services;

public class IngredientService : BaseService<IngredientReadDto, IngredientCreateDto, IngredientUpdateDto, Ingredient>
{
    private readonly IngredientRepository _ingredients;
    private readonly PlatService _plats;
    private readonly ListeService _listes;

    public IngredientService(
        IngredientRepository repository,
        PlatService plats,
        ListeService listes,
        IHttpContextAccessor httpContextAccessor)
        : base(repository, httpContextAccessor)
    {
        _ingredients = repository;
        _plats = plats;
        _listes = listes;
    }

    protected override IngredientReadDto MapToReadDto(Ingredient entity) => entity.ToReadDto();
    protected override Ingredient MapToEntity(IngredientCreateDto dto) => dto.ToModel();
    protected override void ApplyUpdate(Ingredient entity, IngredientUpdateDto dto) => entity.Apply(dto);

    public override async Task<IngredientReadDto> CreateAsync(IngredientCreateDto dto, CancellationToken ct = default)
    {
        EnsureAuthenticated();

        var entity = dto.ToModel();
        ApplyNormalizedName(entity, dto.Name);
        await EnsureNameAvailableAsync(entity.Name, entity.NameKey!, excludeId: null, ct);

        try
        {
            await _ingredients.CreateAsync(entity, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicateNameException(ex);
        }

        return entity.ToReadDto();
    }

    public override async Task<IngredientReadDto?> UpdateAsync(string id, IngredientUpdateDto dto, CancellationToken ct = default)
    {
        EnsureAuthenticated();

        var existing = await _ingredients.GetByIdAsync(id, ct);
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

        if (dto.Aisle is not null)
        {
            existing.Aisle = dto.Aisle;
        }

        try
        {
            await _ingredients.UpdateAsync(id, existing, ct);
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

        await _plats.CascadeRemoveIngredientAsync(id, ct);
        await _listes.CascadeRemoveIngredientAsync(id, ct);
        return true;
    }

    private async Task EnsureNameAvailableAsync(
        string normalizedName,
        string nameKey,
        string? excludeId,
        CancellationToken ct)
    {
        var namePattern = NameNormalizer.ToExactFlexibleWhitespacePattern(normalizedName);
        var duplicate = await _ingredients.FindByNameKeyOrNamePatternAsync(nameKey, namePattern, excludeId, ct);
        if (duplicate is not null)
        {
            throw DuplicateNameException();
        }
    }

    private static void ApplyNormalizedName(Ingredient entity, string name)
    {
        entity.Name = NameNormalizer.NormalizeDisplayName(name);
        entity.NameKey = NameNormalizer.ToKey(entity.Name);
    }

    private static DomainException DuplicateNameException(Exception? inner = null)
    {
        const string message = "Un ingrédient avec ce nom existe déjà.";
        return inner is null
            ? new DomainException(message, code: "INGREDIENT_DUPLICATE", httpStatus: HttpStatusCode.Conflict)
            : new DomainException(message, inner, code: "INGREDIENT_DUPLICATE", httpStatus: HttpStatusCode.Conflict);
    }
}
