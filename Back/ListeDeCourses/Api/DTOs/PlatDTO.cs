using System.ComponentModel.DataAnnotations;

namespace ListeDeCourses.Api.DTOs
{
    public record PlatIngredientDto
    {
        public required string IngredientId { get; init; }

        [Range(0.0, double.MaxValue)]
        public double? Quantity { get; init; }

        [MaxLength(DtoConstraints.UnitMax)]
        [RegularExpression("^(g|kg|paquet)$", ErrorMessage = "Unit must be one of: g, kg, paquet.")]
        public string? Unit { get; init; }
    }

    public record PlatReadDto
    {
        public required string Id { get; init; }

        [Required, MinLength(DtoConstraints.NameMin), MaxLength(DtoConstraints.NameMax)]
        public required string Name { get; init; }

        public required List<PlatIngredientDto> Ingredients { get; init; }
    }

    public record PlatCreateDto
    {
        [Required, MinLength(DtoConstraints.NameMin), MaxLength(DtoConstraints.NameMax)]
        public required string Name { get; init; }

        [Required, MinLength(1)]
        public required List<PlatIngredientDto> Ingredients { get; init; }
    }

    public record PlatUpdateDto
    {
        [MinLength(DtoConstraints.NameMin), MaxLength(DtoConstraints.NameMax)]
        public string? Name { get; init; }

        public List<PlatIngredientDto>? Ingredients { get; init; }
    }
}
