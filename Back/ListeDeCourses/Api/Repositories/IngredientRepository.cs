using ListeDeCourses.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ListeDeCourses.Api.Repositories;

public class IngredientRepository : BaseRepository<Ingredient>
{
    public IngredientRepository(IMongoDatabase database)
        : base(database, "ingredients") { }

    public async Task<Ingredient?> FindByNameKeyOrNamePatternAsync(
        string nameKey,
        string namePattern,
        string? excludeId = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(namePattern);

        var filter = Builders<Ingredient>.Filter.Or(
            Builders<Ingredient>.Filter.Eq("nameKey", nameKey),
            Builders<Ingredient>.Filter.Regex("name", new BsonRegularExpression(namePattern, "i")));

        if (!string.IsNullOrWhiteSpace(excludeId))
        {
            filter = Builders<Ingredient>.Filter.And(
                filter,
                Builders<Ingredient>.Filter.Not(BuildIdFilter(excludeId)));
        }

        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }
}
