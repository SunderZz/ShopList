using MongoDB.Bson;
using MongoDB.Driver;

namespace ListeDeCourses.Api.Repositories
{
    public abstract class BaseRepository<T> where T : class
    {
        protected readonly IMongoCollection<T> _collection;

        protected BaseRepository(IMongoDatabase database, string collectionName)
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentException.ThrowIfNullOrEmpty(collectionName);

            _collection = database.GetCollection<T>(collectionName);
        }

        public virtual Task<List<T>> GetAllAsync(CancellationToken ct = default) =>
            _collection.Find(_ => true).ToListAsync(ct);

        public virtual async Task<T?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var filter = BuildIdFilter(id);
            return await _collection.Find(filter).FirstOrDefaultAsync(ct);
        }

        public virtual async Task<T> CreateAsync(T entity, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _collection.InsertOneAsync(entity, cancellationToken: ct);
            return entity;
        }

        public virtual async Task<bool> UpdateAsync(string id, T entity, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var filter = BuildIdFilter(id);
            var result = await _collection.ReplaceOneAsync(filter, entity, cancellationToken: ct);
            return result.ModifiedCount > 0;
        }

        public virtual async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
        {
            var filter = BuildIdFilter(id);
            var result = await _collection.DeleteOneAsync(filter, ct);
            return result.DeletedCount > 0;
        }


        protected static FilterDefinition<T> BuildIdFilter(string id)
        {
            ArgumentException.ThrowIfNullOrEmpty(id);

            if (ObjectId.TryParse(id, out var oid))
            {
                return Builders<T>.Filter.Eq("_id", oid);
            }

            return Builders<T>.Filter.Or(
                Builders<T>.Filter.Eq("_id", id),
                Builders<T>.Filter.Eq("Id", id)
            );
        }
    }
}
