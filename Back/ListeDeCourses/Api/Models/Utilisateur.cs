using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ListeDeCourses.Api.Models;

[BsonIgnoreExtraElements]
public class Utilisateur
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("pseudo")]
    public string Pseudo { get; set; } = string.Empty;

    [BsonElement("isSuperUser")]
    public bool IsSuperUser { get; set; }

    [BsonElement("passwordHash")]
    public string? PasswordHash { get; set; }
}
