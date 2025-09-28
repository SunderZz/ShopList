using System.Globalization;
using System.Security.Claims;
using ListeDeCourses.Api.Common;
using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Mappings;
using ListeDeCourses.Api.Models;
using ListeDeCourses.Api.Repositories;

namespace ListeDeCourses.Api.Services;

public class ListeService : BaseService<ListeReadDto, ListeCreateDto, ListeUpdateDto, Liste>
{
    private readonly PlatRepository _plats;
    private readonly IngredientRepository _ingredients;
    private readonly IHttpContextAccessor _http;

    public ListeService(
        ListeRepository repository,
        PlatRepository plats,
        IngredientRepository ingredients,
        IHttpContextAccessor httpContextAccessor)
        : base(repository)
    {
        _plats = plats;
        _ingredients = ingredients;
        _http = httpContextAccessor;
    }

    protected override ListeReadDto MapToReadDto(Liste entity) => entity.ToReadDto();
    protected override Liste MapToEntity(ListeCreateDto dto) => dto.ToModel();
    protected override void ApplyUpdate(Liste entity, ListeUpdateDto dto) => entity.Apply(dto);

    private string? GetCurrentUserId() =>
        _http.HttpContext?.User?.FindFirst("sub")?.Value
        ?? _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private bool IsSuperUser() =>
        string.Equals(
            _http.HttpContext?.User?.FindFirst("role")?.Value,
            "superuser",
            StringComparison.OrdinalIgnoreCase);

    private void EnsureOwnership(Liste l)
    {
        if (IsSuperUser()) return;
        var uid = GetCurrentUserId();
        if (string.IsNullOrEmpty(uid) || !string.Equals(l.OwnerId, uid, StringComparison.Ordinal))
            throw new DomainException("Accès refusé à cette liste.", code: "LIST_FORBIDDEN", httpStatus: System.Net.HttpStatusCode.Forbidden);
    }

    public override async Task<IEnumerable<ListeReadDto>> GetAllAsync(CancellationToken ct = default)
    {
        var uid = GetCurrentUserId();
        var all = await _repository.GetAllAsync(ct);

        var filtered = IsSuperUser() || string.IsNullOrEmpty(uid)
            ? all
            : all.Where(l => l.OwnerId == uid);

        return filtered.Select(MapToReadDto);
    }

    public override async Task<ListeReadDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return null;

        EnsureOwnership(existing);
        return existing.ToReadDto();
    }

    public override async Task<ListeReadDto> CreateAsync(ListeCreateDto dto, CancellationToken ct = default)
    {
        var uid = GetCurrentUserId()
                  ?? throw new DomainException("Utilisateur non authentifié.", code: "AUTH_REQUIRED", httpStatus: System.Net.HttpStatusCode.Unauthorized);

        var entity = dto.ToModel();
        entity.OwnerId = uid;

        await MaterializeAsync(entity, manualFromDto: dto.Items, dishIdsFromDto: dto.DishIds, keepCheckedFrom: null, ct);
        await _repository.CreateAsync(entity, ct);
        return entity.ToReadDto();
    }

    public override async Task<ListeReadDto?> UpdateAsync(string id, ListeUpdateDto dto, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return default;

        EnsureOwnership(existing);

        if (dto.DishIds is not null) existing.DishIds = dto.DishIds;

        await MaterializeAsync(
            existing,
            manualFromDto: dto.Items,
            dishIdsFromDto: dto.DishIds,
            keepCheckedFrom: existing,
            ct: ct
        );

        await _repository.UpdateAsync(id, existing, ct);
        return existing.ToReadDto();
    }

    public override async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return false;

        EnsureOwnership(existing);
        return await _repository.DeleteAsync(id, ct);
    }

    public async Task CascadeRemoveDishAsync(string dishId, CancellationToken ct = default)
    {
        var all = await _repository.GetAllAsync(ct);
        foreach (var l in all.Where(x => x.DishIds.Contains(dishId)))
        {
            l.DishIds.RemoveAll(x => x == dishId);
            await MaterializeAsync(l, manualFromDto: null, dishIdsFromDto: l.DishIds, keepCheckedFrom: l, ct);
            await _repository.UpdateAsync(l.Id, l, ct);
        }
    }

    public async Task CascadeRemoveIngredientAsync(string ingredientId, CancellationToken ct = default)
    {
        var all = await _repository.GetAllAsync(ct);
        foreach (var l in all.Where(x => x.Items.Any(i => i.IngredientId == ingredientId)))
        {
            l.Items.RemoveAll(i => i.IngredientId == ingredientId);
            await _repository.UpdateAsync(l.Id, l, ct);
        }
    }

    private async Task MaterializeAsync(
        Liste list,
        List<ListeItemDto>? manualFromDto,
        List<string>? dishIdsFromDto,
        Liste? keepCheckedFrom,
        CancellationToken ct)
    {
        var checkedMap = keepCheckedFrom?.Items.ToDictionary(x => x.IngredientId, x => x.Checked) ?? new();
        if (manualFromDto is not null)
        {
            foreach (var m in manualFromDto)
            {
                if (m.Checked.HasValue) checkedMap[m.IngredientId] = m.Checked.Value;
            }
        }

        if (dishIdsFromDto is not null)
            list.DishIds = dishIdsFromDto;

        var acc = await AggregateFromDishesAsync(list.DishIds, ct);

        if (manualFromDto is not null && manualFromDto.Count > 0)
            await MergeManualAsync(acc, manualFromDto, ct);

        var final = new List<ListeItem>(acc.Count);
        foreach (var kv in acc.Values.OrderBy(v => v.name, StringComparer.Create(CultureInfo.GetCultureInfo("fr-FR"), ignoreCase: true)))
        {
            final.Add(new ListeItem
            {
                IngredientId = kv.ingredientId,
                IngredientName = kv.name,
                Aisle = kv.aisle,
                Quantity = kv.qty,
                Unit = kv.unit,
                Checked = checkedMap.TryGetValue(kv.ingredientId, out var c) ? c : false
            });
        }

        list.Items = final;
    }

    private async Task<Dictionary<string, (string ingredientId, string name, string? aisle, double? qty, string? unit)>> AggregateFromDishesAsync(
        List<string> dishIds,
        CancellationToken ct)
    {
        var acc = new Dictionary<string, (string ingredientId, string name, string? aisle, double? qty, string? unit)>();

        foreach (var dId in (dishIds ?? new()).Distinct())
        {
            var dish = await _plats.GetByIdAsync(dId, ct);
            if (dish is null) continue;

            foreach (var di in dish.Ingredients)
            {
                var ing = await _ingredients.GetByIdAsync(di.IngredientId, ct);
                if (ing is null) continue;

                if (!acc.TryGetValue(di.IngredientId, out var cur))
                {
                    acc[di.IngredientId] = (di.IngredientId, ing.Name, ing.Aisle, di.Quantity, di.Unit);
                }
                else
                {
                    var curUnit = cur.unit?.Trim();
                    var newUnit = di.Unit?.Trim();
                    if (!string.IsNullOrEmpty(curUnit) && !string.IsNullOrEmpty(newUnit) &&
                        string.Equals(curUnit, newUnit, StringComparison.OrdinalIgnoreCase))
                    {
                        acc[di.IngredientId] = (cur.ingredientId, cur.name, cur.aisle, (cur.qty ?? 0) + (di.Quantity ?? 0), curUnit);
                    }
                    else
                    {
                        acc[di.IngredientId] = (cur.ingredientId, cur.name, cur.aisle, null, null);
                    }
                }
            }
        }

        return acc;
    }

    private async Task MergeManualAsync(
        Dictionary<string, (string ingredientId, string name, string? aisle, double? qty, string? unit)> acc,
        List<ListeItemDto> manual,
        CancellationToken ct)
    {
        foreach (var m in manual)
        {
            var ing = await _ingredients.GetByIdAsync(m.IngredientId, ct);

            var mName = ing?.Name ?? m.IngredientName;
            var mAisle = ing?.Aisle ?? m.Aisle;
            var mQty = m.Quantity;
            var mUnit = string.IsNullOrWhiteSpace(m.Unit) ? null : m.Unit!.Trim();

            if (!acc.TryGetValue(m.IngredientId, out var cur))
            {
                acc[m.IngredientId] = (m.IngredientId, mName, mAisle, mQty, mUnit);
            }
            else
            {
                var curUnit = cur.unit?.Trim();
                if (!string.IsNullOrEmpty(curUnit) && !string.IsNullOrEmpty(mUnit) &&
                    string.Equals(curUnit, mUnit, StringComparison.OrdinalIgnoreCase))
                {
                    acc[m.IngredientId] = (cur.ingredientId, mName, mAisle, (cur.qty ?? 0) + (mQty ?? 0), curUnit);
                }
                else if (cur.qty is null && cur.unit is null)
                {
                }
                else
                {
                    acc[m.IngredientId] = (cur.ingredientId, mName, mAisle, null, null);
                }
            }
        }
    }
}
