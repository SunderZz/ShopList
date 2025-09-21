using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ListeDeCourses.Api.Models;

[BsonIgnoreExtraElements]
public class Liste
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [Required, MinLength(1), MaxLength(120)]
    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required]
    [BsonElement("items")]
    public List<ListeItem> Items { get; set; } = new();

    [BsonElement("dishIds")]
    public List<string> DishIds { get; set; } = new();

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("ownerId")]
    public string OwnerId { get; set; } = null!;
}

[BsonIgnoreExtraElements]
public class ListeItem
{
    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("ingredientId")]
    public string IngredientId { get; set; } = null!;

    [Required, MinLength(1), MaxLength(100)]
    [BsonElement("ingredientName")]
    public string IngredientName { get; set; } = null!;

    [Range(0.0, double.MaxValue)]
    [BsonElement("quantity")]
    public double? Quantity { get; set; }

    [MaxLength(50)]
    [BsonElement("unit")]
    public string? Unit { get; set; }

    [MaxLength(100)]
    [BsonElement("aisle")]
    public string? Aisle { get; set; }

    [BsonElement("checked")]
    public bool Checked { get; set; }
}
