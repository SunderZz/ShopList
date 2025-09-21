using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ListeDeCourses.Api.Models;

[BsonIgnoreExtraElements]
public class Plat
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [Required, MinLength(1), MaxLength(120)]
    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [Required]
    [BsonElement("ingredients")]
    public List<PlatIngredient> Ingredients { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class PlatIngredient
{
    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("ingredientId")]
    public string IngredientId { get; set; } = null!;

    [Range(0.0, double.MaxValue)]
    [BsonElement("quantity")]
    public double? Quantity { get; set; }

    [MaxLength(50)]
    [BsonElement("unit")]
    public string? Unit { get; set; }
}
