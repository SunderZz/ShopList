using System.Linq;
using ListeDeCourses.Api.Common;
using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Models;

namespace ListeDeCourses.Api.Mappings;

public static class ListeMappings
{
    public static ListeReadDto ToReadDto(this Liste m) => new()
    {
        Id          = m.Id,
        Name        = m.Name,
        Date        = m.Date,
        Items       = m.Items?.Select(ToDtoItem).ToList() ?? new(),
        ManualItems = m.ManualItems?.Select(ToDtoItem).ToList() ?? new(),
        DishIds     = m.DishIds ?? new(),
        OwnerId     = m.EffectiveOwnerId ?? string.Empty
    };

    public static Liste ToModel(this ListeCreateDto dto) => new()
    {
        Name        = dto.Name,
        Date        = dto.Date ?? DateTime.UtcNow,
        Items       = new(),
        ManualItems = dto.Items?.Select(ToModelItem).ToList() ?? new(),
        DishIds     = dto.DishIds ?? new()
    };

    public static void Apply(this Liste m, ListeUpdateDto dto)
    {
        if (dto.Name is not null) m.Name = dto.Name;
        if (dto.Date.HasValue)    m.Date = dto.Date.Value;
        if (dto.Items is not null)   m.ManualItems = dto.Items.Select(ToModelItem).ToList();
        if (dto.DishIds is not null) m.DishIds     = dto.DishIds;
    }

    private static ListeItemDto ToDtoItem(ListeItem i) => new()
    {
        IngredientId   = i.IngredientId,
        IngredientName = i.IngredientName,
        Quantity       = i.Quantity,
        Quantities     = i.Quantities?.Select(ToDtoQuantity).ToList() ?? new(),
        Unit           = i.Unit,
        Aisle          = i.Aisle,
        Checked        = i.Checked
    };

    private static ListeItem ToModelItem(ListeItemDto d) => new()
    {
        IngredientId   = d.IngredientId,
        IngredientName = d.IngredientName,
        Quantity       = d.Quantity,
        Quantities     = d.Quantities?.Select(ToModelQuantity).ToList() ?? new(),
        Unit           = UnitCatalog.Normalize(d.Unit),
        Aisle          = d.Aisle,
        Checked        = d.Checked ?? false
    };

    private static ListeItemQuantityDto ToDtoQuantity(ListeItemQuantity q) => new()
    {
        Quantity = q.Quantity,
        Unit = q.Unit
    };

    private static ListeItemQuantity ToModelQuantity(ListeItemQuantityDto q) => new()
    {
        Quantity = q.Quantity,
        Unit = UnitCatalog.Normalize(q.Unit)
    };
}
