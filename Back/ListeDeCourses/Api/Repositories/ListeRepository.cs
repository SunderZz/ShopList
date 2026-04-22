    using ListeDeCourses.Api.Models;
    using MongoDB.Driver;

    namespace ListeDeCourses.Api.Repositories;

    public class ListeRepository : BaseRepository<Liste>
    {
        public ListeRepository(IMongoDatabase database)
            : base(database, "listes") { }

        public Task<List<Liste>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
            return _collection.Find(x => x.OwnerId == ownerId).ToListAsync(ct);
        }
    }
