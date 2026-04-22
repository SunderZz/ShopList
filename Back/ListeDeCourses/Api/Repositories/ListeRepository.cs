using ListeDeCourses.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ListeDeCourses.Api.Repositories;

public class ListeRepository : BaseRepository<Liste>
{
    public ListeRepository(IMongoDatabase database)
        : base(database, "listes") { }

    public Task<List<Liste>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var ownerFilters = new List<FilterDefinition<Liste>>
        {
            Builders<Liste>.Filter.Eq("ownerId", ownerId),
            Builders<Liste>.Filter.Eq("userId", ownerId)
        };

        if (ObjectId.TryParse(ownerId, out var ownerObjectId))
        {
            ownerFilters.Add(Builders<Liste>.Filter.Eq("ownerId", ownerObjectId));
            ownerFilters.Add(Builders<Liste>.Filter.Eq("userId", ownerObjectId));
        }

        return _collection.Find(Builders<Liste>.Filter.Or(ownerFilters)).ToListAsync(ct);
    }
}
