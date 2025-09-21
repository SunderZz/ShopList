using System.ComponentModel.DataAnnotations;

namespace ListeDeCourses.Api.DTOs
{
    public record IngredientReadDto
    {
        public required string Id { get; init; }

        [Required, MinLength(DtoConstraints.NameMin), MaxLength(DtoConstraints.IngredientNameMax)]
        public required string Name { get; init; }

        [MaxLength(DtoConstraints.AisleMax)]
        public string? Aisle { get; init; }
    }

    public record IngredientCreateDto
    {
        [Required, MinLength(DtoConstraints.NameMin), MaxLength(DtoConstraints.IngredientNameMax)]
        public required string Name { get; init; }

        [MaxLength(DtoConstraints.AisleMax)]
        public string? Aisle { get; init; }
    }

    public record IngredientUpdateDto
    {
        [MinLength(DtoConstraints.NameMin), MaxLength(DtoConstraints.IngredientNameMax)]
        public string? Name { get; init; }

        [MaxLength(DtoConstraints.AisleMax)]
        public string? Aisle { get; init; }
    }
}
