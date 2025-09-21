using System.Linq;
using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Models;

namespace ListeDeCourses.Api.Mappings;

public static class ListeMappings
{
    public static ListeReadDto ToReadDto(this Liste m) => new()
    {
        Id      = m.Id,
        Name    = m.Name,
        Date    = m.Date,
        Items   = m.Items?.Select(ToDtoItem).ToList() ?? new(),
        DishIds = m.DishIds ?? new(),
        OwnerId = m.OwnerId
    };

    public static Liste ToModel(this ListeCreateDto dto) => new()
    {
        Name    = dto.Name,
        Date    = dto.Date ?? DateTime.UtcNow,
        Items   = dto.Items?.Select(ToModelItem).ToList() ?? new(),
        DishIds = dto.DishIds ?? new()
    };

    public static void Apply(this Liste m, ListeUpdateDto dto)
    {
        if (dto.Name is not null) m.Name = dto.Name;
        if (dto.Date.HasValue)    m.Date = dto.Date.Value;
        if (dto.Items is not null)   m.Items   = dto.Items.Select(ToModelItem).ToList();
        if (dto.DishIds is not null) m.DishIds = dto.DishIds;
    }

    private static ListeItemDto ToDtoItem(ListeItem i) => new()
    {
        IngredientId   = i.IngredientId,
        IngredientName = i.IngredientName,
        Quantity       = i.Quantity,
        Unit           = i.Unit,
        Aisle          = i.Aisle,
        Checked        = i.Checked
    };

    private static ListeItem ToModelItem(ListeItemDto d) => new()
    {
        IngredientId   = d.IngredientId,
        IngredientName = d.IngredientName,
        Quantity       = d.Quantity,
        Unit           = d.Unit,
        Aisle          = d.Aisle,
        Checked        = d.Checked ?? false
    };
}
