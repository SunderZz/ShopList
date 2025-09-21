    using ListeDeCourses.Api.Models;
    using MongoDB.Driver;

    namespace ListeDeCourses.Api.Repositories;

    public class ListeRepository : BaseRepository<Liste>
    {
        public ListeRepository(IMongoDatabase database)
            : base(database, "listes") { }
    }
