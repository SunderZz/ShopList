using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ListeDeCourses.Api.Models;

[BsonIgnoreExtraElements]
public class Ingredient
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [Required, MinLength(1), MaxLength(100)]
    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    [BsonElement("aisle")]
    public string? Aisle { get; set; }
}
