using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Mappings;
using ListeDeCourses.Api.Models;
using ListeDeCourses.Api.Repositories;

namespace ListeDeCourses.Api.Services;

public class IngredientService : BaseService<IngredientReadDto, IngredientCreateDto, IngredientUpdateDto, Ingredient>
{
    private readonly PlatService _plats;
    private readonly ListeService _listes;

    public IngredientService(IngredientRepository repository, PlatService plats, ListeService listes) : base(repository)
    {
        _plats = plats;
        _listes = listes;
    }

    protected override IngredientReadDto MapToReadDto(Ingredient entity) => entity.ToReadDto();
    protected override Ingredient MapToEntity(IngredientCreateDto dto) => dto.ToModel();
    protected override void ApplyUpdate(Ingredient entity, IngredientUpdateDto dto) => entity.Apply(dto);

    public override async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var ok = await _repository.DeleteAsync(id, ct);
        if (!ok) return false;

        await _plats.CascadeRemoveIngredientAsync(id, ct);
        await _listes.CascadeRemoveIngredientAsync(id, ct);
        return true;
    }
}
