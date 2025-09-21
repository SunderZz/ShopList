using System.Linq;
using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Models;

namespace ListeDeCourses.Api.Mappings;

public static class PlatMappings
{
    public static PlatReadDto ToReadDto(this Plat m) => new()
    {
        Id          = m.Id,
        Name        = m.Name,
        Ingredients = m.Ingredients?.Select(ToDtoIngredient).ToList() ?? new()
    };

    public static Plat ToModel(this PlatCreateDto dto) => new()
    {
        Name        = dto.Name,
        Ingredients = dto.Ingredients?.Select(ToModelIngredient).ToList() ?? new()
    };

    public static void Apply(this Plat m, PlatUpdateDto dto)
    {
        if (dto.Name is not null) m.Name = dto.Name;
        if (dto.Ingredients is not null)
            m.Ingredients = dto.Ingredients.Select(ToModelIngredient).ToList();
    }

    private static PlatIngredientDto ToDtoIngredient(PlatIngredient i) => new()
    {
        IngredientId = i.IngredientId,
        Quantity     = i.Quantity,
        Unit         = i.Unit
    };

    private static PlatIngredient ToModelIngredient(PlatIngredientDto d) => new()
    {
        IngredientId = d.IngredientId,
        Quantity     = d.Quantity,
        Unit         = d.Unit
    };
}
