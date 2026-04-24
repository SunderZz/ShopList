using System.Globalization;
using System.Security.Claims;
using ListeDeCourses.Api.Common;
using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Mappings;
using ListeDeCourses.Api.Models;
using ListeDeCourses.Api.Repositories;

namespace ListeDeCourses.Api.Services;

public class ListeService
    : BaseService<ListeReadDto, ListeCreateDto, ListeUpdateDto, Liste>,
      IItemCheckService<ListeReadDto>
{
    private sealed class AggregatedListItem
    {
        public required string IngredientId { get; init; }
        public required string Name { get; set; }
        public string? Aisle { get; set; }
        public bool HasAmbiguousQuantity { get; set; }
        public Dictionary<string, ListeItemQuantity> QuantitiesByUnit { get; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    private readonly ListeRepository _listes;
    private readonly PlatRepository _plats;
    private readonly IngredientRepository _ingredients;
    private readonly IHttpContextAccessor _http;

    public ListeService(
        ListeRepository repository,
        PlatRepository plats,
        IngredientRepository ingredients,
        IHttpContextAccessor httpContextAccessor)
        : base(repository, httpContextAccessor)
    {
        _listes = repository;
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

    private void EnsureOwnership(Liste list)
    {
        if (IsSuperUser()) return;

        var uid = GetCurrentUserId();
        if (string.IsNullOrEmpty(uid) || !string.Equals(list.EffectiveOwnerId, uid, StringComparison.Ordinal))
        {
            throw new DomainException(
                "Acc\u00E8s refus\u00E9 \u00E0 cette liste.",
                code: "LIST_FORBIDDEN",
                httpStatus: System.Net.HttpStatusCode.Forbidden);
        }
    }

    public override async Task<IEnumerable<ListeReadDto>> GetAllAsync(CancellationToken ct = default)
    {
        EnsureAuthenticated();
        if (IsSuperUser())
        {
            var lists = await _listes.GetAllAsync(ct);
            foreach (var list in lists)
                await HydrateLegacyManualItemsAsync(list, ct);
            return lists.Select(MapToReadDto);
        }

        var uid = GetCurrentUserId()
                  ?? throw new DomainException(
                      "Utilisateur non authentifi\u00E9.",
                      code: "AUTH_REQUIRED",
                      httpStatus: System.Net.HttpStatusCode.Unauthorized);

        var ownedLists = await _listes.GetByOwnerIdAsync(uid, ct);
        foreach (var list in ownedLists)
            await HydrateLegacyManualItemsAsync(list, ct);
        return ownedLists.Select(MapToReadDto);
    }

    public override async Task<ListeReadDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return null;

        EnsureOwnership(existing);
        await HydrateLegacyManualItemsAsync(existing, ct);
        return existing.ToReadDto();
    }

    public override async Task<ListeReadDto> CreateAsync(ListeCreateDto dto, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        var uid = GetCurrentUserId()
                  ?? throw new DomainException(
                      "Utilisateur non authentifi\u00E9.",
                      code: "AUTH_REQUIRED",
                      httpStatus: System.Net.HttpStatusCode.Unauthorized);

        var entity = dto.ToModel();
        entity.OwnerId = uid;
        entity.LegacyUserId = null;

        await MaterializeAsync(entity, manualFromDto: dto.Items, dishIdsFromDto: dto.DishIds, keepCheckedFrom: null, ct);
        await _repository.CreateAsync(entity, ct);
        return entity.ToReadDto();
    }

    public override async Task<ListeReadDto?> UpdateAsync(string id, ListeUpdateDto dto, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return default;

        EnsureOwnership(existing);
        await HydrateLegacyManualItemsAsync(existing, ct);

        if (dto.DishIds is not null)
            existing.DishIds = dto.DishIds;

        await MaterializeAsync(
            existing,
            manualFromDto: dto.Items,
            dishIdsFromDto: dto.DishIds,
            keepCheckedFrom: existing,
            ct: ct);

        await _repository.UpdateAsync(id, existing, ct);
        return existing.ToReadDto();
    }

    public override async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return false;

        EnsureOwnership(existing);
        return await _repository.DeleteAsync(id, ct);
    }

    public async Task CascadeRemoveDishAsync(string dishId, CancellationToken ct = default)
    {
        var all = await _repository.GetAllAsync(ct);
        foreach (var list in all.Where(x => x.DishIds.Contains(dishId)))
        {
            list.DishIds.RemoveAll(x => x == dishId);
            await MaterializeAsync(list, manualFromDto: null, dishIdsFromDto: list.DishIds, keepCheckedFrom: list, ct);
            await _repository.UpdateAsync(list.Id, list, ct);
        }
    }

    public async Task CascadeRemoveIngredientAsync(string ingredientId, CancellationToken ct = default)
    {
        var all = await _repository.GetAllAsync(ct);
        foreach (var list in all.Where(x =>
                     x.Items.Any(i => i.IngredientId == ingredientId) ||
                     x.ManualItems.Any(i => i.IngredientId == ingredientId)))
        {
            list.Items.RemoveAll(i => i.IngredientId == ingredientId);
            list.ManualItems.RemoveAll(i => i.IngredientId == ingredientId);
            await MaterializeAsync(list, manualFromDto: null, dishIdsFromDto: list.DishIds, keepCheckedFrom: list, ct);
            await _repository.UpdateAsync(list.Id, list, ct);
        }
    }

    public async Task<ListeReadDto?> SetItemCheckedAsync(string listId, string ingredientId, bool isChecked, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        var existing = await _repository.GetByIdAsync(listId, ct);
        if (existing is null) return null;

        EnsureOwnership(existing);

        var item = existing.Items.FirstOrDefault(i => i.IngredientId == ingredientId);
        if (item is null) return null;

        item.Checked = isChecked;

        await _repository.UpdateAsync(listId, existing, ct);
        return existing.ToReadDto();
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
            list.ManualItems = manualFromDto.Select(ToStoredManualItem).ToList();
            foreach (var manual in manualFromDto)
            {
                if (manual.Checked.HasValue)
                    checkedMap[manual.IngredientId] = manual.Checked.Value;
            }
        }

        if (dishIdsFromDto is not null)
            list.DishIds = dishIdsFromDto.Distinct(StringComparer.Ordinal).ToList();

        var acc = await AggregateFromDishesAsync(list.DishIds, ct);

        if (list.ManualItems.Count > 0)
            await MergeManualAsync(acc, list.ManualItems, ct);

        var final = new List<ListeItem>(acc.Count);
        foreach (var aggregate in acc.Values.OrderBy(
                     value => value.Name,
                     StringComparer.Create(CultureInfo.GetCultureInfo("fr-FR"), ignoreCase: true)))
        {
            var quantities = BuildQuantities(aggregate);
            var primary = quantities.Count == 1 ? quantities[0] : null;

            final.Add(new ListeItem
            {
                IngredientId = aggregate.IngredientId,
                IngredientName = aggregate.Name,
                Aisle = aggregate.Aisle,
                Quantity = primary?.Quantity,
                Quantities = quantities,
                Unit = primary?.Unit,
                Checked = checkedMap.TryGetValue(aggregate.IngredientId, out var isChecked) && isChecked
            });
        }

        list.Items = final;
    }

    private async Task<Dictionary<string, AggregatedListItem>> AggregateFromDishesAsync(
        List<string> dishIds,
        CancellationToken ct)
    {
        var acc = new Dictionary<string, AggregatedListItem>(StringComparer.Ordinal);
        var distinctDishIds = (dishIds ?? new())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (distinctDishIds.Count == 0) return acc;

        var dishes = await _plats.GetByIdsAsync(distinctDishIds, ct);
        var ingredientIds = dishes
            .SelectMany(dish => dish.Ingredients)
            .Select(ingredient => ingredient.IngredientId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal);
        var ingredientsById = (await _ingredients.GetByIdsAsync(
            ingredientIds,
            ct))
            .ToDictionary(ingredient => ingredient.Id, StringComparer.Ordinal);

        foreach (var dish in dishes)
        {
            foreach (var dishIngredient in dish.Ingredients)
            {
                if (!ingredientsById.TryGetValue(dishIngredient.IngredientId, out var ingredient)) continue;

                var aggregate = GetOrCreateAggregate(acc, dishIngredient.IngredientId, ingredient.Name, ingredient.Aisle);
                AddQuantity(aggregate, dishIngredient.Quantity, dishIngredient.Unit);
            }
        }

        return acc;
    }

    private async Task MergeManualAsync(
        Dictionary<string, AggregatedListItem> acc,
        List<ListeItem> manual,
        CancellationToken ct)
    {
        var ingredientsById = (await _ingredients.GetByIdsAsync(
            manual
                .Select(manualItem => manualItem.IngredientId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal),
            ct))
            .ToDictionary(ingredient => ingredient.Id, StringComparer.Ordinal);

        foreach (var manualItem in manual)
        {
            ingredientsById.TryGetValue(manualItem.IngredientId, out var ingredient);

            var manualName = ingredient?.Name ?? manualItem.IngredientName;
            var manualAisle = ingredient?.Aisle ?? manualItem.Aisle;
            var aggregate = GetOrCreateAggregate(acc, manualItem.IngredientId, manualName, manualAisle);
            aggregate.Name = manualName;
            aggregate.Aisle = manualAisle;
            AddQuantity(aggregate, manualItem.Quantity, manualItem.Unit);
        }
    }

    private static AggregatedListItem GetOrCreateAggregate(
        Dictionary<string, AggregatedListItem> acc,
        string ingredientId,
        string name,
        string? aisle)
    {
        if (acc.TryGetValue(ingredientId, out var existing))
            return existing;

        var created = new AggregatedListItem
        {
            IngredientId = ingredientId,
            Name = name,
            Aisle = aisle,
        };
        acc[ingredientId] = created;
        return created;
    }

    private static void AddQuantity(AggregatedListItem aggregate, double? quantity, string? unit)
    {
        if (!UnitCatalog.TryConvertToCanonical(quantity, unit, out var canonicalQuantity, out var canonicalUnit))
        {
            aggregate.HasAmbiguousQuantity = true;
            return;
        }

        if (!aggregate.QuantitiesByUnit.TryGetValue(canonicalUnit, out var existing))
        {
            aggregate.QuantitiesByUnit[canonicalUnit] = new ListeItemQuantity
            {
                Quantity = canonicalQuantity,
                Unit = canonicalUnit
            };
            return;
        }

        existing.Quantity = (existing.Quantity ?? 0) + canonicalQuantity;
    }

    private static List<ListeItemQuantity> BuildQuantities(AggregatedListItem aggregate)
    {
        if (aggregate.HasAmbiguousQuantity)
            return new();

        return aggregate.QuantitiesByUnit.Values
            .OrderBy(quantity => UnitCatalog.GetSortOrder(quantity.Unit))
            .ThenBy(quantity => quantity.Unit, StringComparer.OrdinalIgnoreCase)
            .Select(quantity => new ListeItemQuantity
            {
                Quantity = quantity.Quantity,
                Unit = quantity.Unit
            })
            .ToList();
    }

    private static ListeItem ToStoredManualItem(ListeItemDto item) => new()
    {
        IngredientId = item.IngredientId,
        IngredientName = item.IngredientName,
        Quantity = item.Quantity,
        Quantities = new(),
        Unit = UnitCatalog.Normalize(item.Unit),
        Aisle = item.Aisle,
        Checked = item.Checked ?? false
    };

    private async Task HydrateLegacyManualItemsAsync(Liste list, CancellationToken ct)
    {
        if (list.ManualItems.Count > 0 || list.Items.Count == 0)
            return;

        var aggregateByIngredientId = await AggregateFromDishesAsync(list.DishIds, ct);
        var inferred = new List<ListeItem>();

        foreach (var item in list.Items)
        {
            if (!aggregateByIngredientId.TryGetValue(item.IngredientId, out var aggregate))
            {
                inferred.Add(ToLegacyManualItem(item, item.Quantity, item.Unit));
                continue;
            }

            var dishQuantities = BuildQuantities(aggregate);
            if (dishQuantities.Count != 1 || item.Quantity is null || string.IsNullOrWhiteSpace(item.Unit))
                continue;

            var dishQuantity = dishQuantities[0];
            if (!string.Equals(dishQuantity.Unit, item.Unit, StringComparison.OrdinalIgnoreCase))
                continue;

            var remainingQuantity = item.Quantity.Value - (dishQuantity.Quantity ?? 0);
            if (remainingQuantity <= 0) continue;

            inferred.Add(ToLegacyManualItem(item, remainingQuantity, item.Unit));
        }

        if (inferred.Count > 0)
            list.ManualItems = inferred;
    }

    private static ListeItem ToLegacyManualItem(ListeItem item, double? quantity, string? unit) => new()
    {
        IngredientId = item.IngredientId,
        IngredientName = item.IngredientName,
        Quantity = quantity,
        Quantities = new(),
        Unit = UnitCatalog.Normalize(unit),
        Aisle = item.Aisle,
        Checked = item.Checked
    };
}
