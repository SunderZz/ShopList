using System.ComponentModel.DataAnnotations;

namespace ListeDeCourses.Api.DTOs
{
    public record ListeItemDto
    {
        public required string IngredientId { get; init; }

        [Required, MinLength(DtoConstraints.NameMin), MaxLength(DtoConstraints.IngredientNameMax)]
        public required string IngredientName { get; init; }

        [Range(0.0, double.MaxValue)]
        public double? Quantity { get; init; }

        [MaxLength(DtoConstraints.UnitMax)]
        [RegularExpression("^(g|kg|paquet|unité)$", ErrorMessage = "Unit must be one of: g, kg, paquet,unité.")]
        public string? Unit { get; init; }

        [MaxLength(DtoConstraints.AisleMax)]
        public string? Aisle { get; init; }

        public bool? Checked { get; init; }
    }

    public record ListeReadDto
    {
        public required string Id { get; init; }

        [Required, MinLength(DtoConstraints.NameMin), MaxLength(DtoConstraints.NameMax)]
        public required string Name { get; init; }

        public required DateTime Date { get; init; }

        public required List<ListeItemDto> Items { get; init; }

        public required List<string> DishIds { get; init; }

        public required string OwnerId { get; init; }
    }

    public record ListeCreateDto
    {
        [Required, MinLength(DtoConstraints.NameMin), MaxLength(DtoConstraints.NameMax)]
        public required string Name { get; init; }

        public DateTime? Date { get; init; }

        public List<ListeItemDto>? Items { get; init; }

        public List<string>? DishIds { get; init; }
    }

    public record ListeUpdateDto
    {
        [MinLength(DtoConstraints.NameMin), MaxLength(DtoConstraints.NameMax)]
        public string? Name { get; init; }

        public DateTime? Date { get; init; }

        public List<ListeItemDto>? Items { get; init; }

        public List<string>? DishIds { get; init; }
    }
}
