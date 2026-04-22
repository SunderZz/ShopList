using ListeDeCourses.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ListeDeCourses.Api.Repositories;

public class PlatRepository : BaseRepository<Plat>
{
    public PlatRepository(IMongoDatabase database)
        : base(database, "plats") { }

    public async Task<Plat?> FindByNameKeyOrNamePatternAsync(
        string nameKey,
        string namePattern,
        string? excludeId = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(namePattern);

        var filter = Builders<Plat>.Filter.Or(
            Builders<Plat>.Filter.Eq("nameKey", nameKey),
            Builders<Plat>.Filter.Regex("name", new BsonRegularExpression(namePattern, "i")));

        if (!string.IsNullOrWhiteSpace(excludeId))
        {
            filter = Builders<Plat>.Filter.And(
                filter,
                Builders<Plat>.Filter.Not(BuildIdFilter(excludeId)));
        }

        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }
}
