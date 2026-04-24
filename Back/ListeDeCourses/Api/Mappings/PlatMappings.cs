using System.Linq;
using ListeDeCourses.Api.Common;
using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Models;

namespace ListeDeCourses.Api.Mappings;

public static class PlatMappings
{
    public static PlatReadDto ToReadDto(this Plat m) => new()
    {
        Id          = m.Id,
        Name        = m.Name,
        SourceUrl   = m.SourceUrl,
        Ingredients = m.Ingredients?.Select(ToDtoIngredient).ToList() ?? new()
    };

    public static Plat ToModel(this PlatCreateDto dto) => new()
    {
        Name        = dto.Name,
        SourceUrl   = NormalizeSourceUrl(dto.SourceUrl),
        Ingredients = dto.Ingredients?.Select(ToModelIngredient).ToList() ?? new()
    };

    public static void Apply(this Plat m, PlatUpdateDto dto)
    {
        if (dto.Name is not null) m.Name = dto.Name;
        if (dto.SourceUrl is not null) m.SourceUrl = NormalizeSourceUrl(dto.SourceUrl);
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
        Unit         = UnitCatalog.Normalize(d.Unit)
    };

    public static string? NormalizeSourceUrl(string? sourceUrl)
    {
        var trimmed = sourceUrl?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
