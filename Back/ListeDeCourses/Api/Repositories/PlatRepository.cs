using ListeDeCourses.Api.Models;
using MongoDB.Driver;

namespace ListeDeCourses.Api.Repositories;

public class PlatRepository : BaseRepository<Plat>
{
    public PlatRepository(IMongoDatabase database)
        : base(database, "plats") { }
}
