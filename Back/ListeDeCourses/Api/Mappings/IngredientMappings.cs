using System.Linq;
using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Models;

namespace ListeDeCourses.Api.Mappings;

public static class IngredientMappings
{
    public static IngredientReadDto ToReadDto(this Ingredient m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Aisle = m.Aisle
    };

    public static Ingredient ToModel(this IngredientCreateDto dto) => new()
    {
        Name = dto.Name,
        Aisle = dto.Aisle
    };

    public static void Apply(this Ingredient m, IngredientUpdateDto dto)
    {
        if (dto.Name is not null)  m.Name  = dto.Name;
        if (dto.Aisle is not null) m.Aisle = dto.Aisle;
    }
}
