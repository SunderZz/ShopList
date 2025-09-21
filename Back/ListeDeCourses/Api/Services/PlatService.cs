using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Mappings;
using ListeDeCourses.Api.Models;
using ListeDeCourses.Api.Repositories;

namespace ListeDeCourses.Api.Services;

public class PlatService : BaseService<PlatReadDto, PlatCreateDto, PlatUpdateDto, Plat>
{
    private readonly ListeService _listes;

    public PlatService(PlatRepository repository, ListeService listes) : base(repository)
    {
        _listes = listes;
    }

    protected override PlatReadDto MapToReadDto(Plat entity) => entity.ToReadDto();
    protected override Plat MapToEntity(PlatCreateDto dto) => dto.ToModel();
    protected override void ApplyUpdate(Plat entity, PlatUpdateDto dto) => entity.Apply(dto);

    public override async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
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
}
