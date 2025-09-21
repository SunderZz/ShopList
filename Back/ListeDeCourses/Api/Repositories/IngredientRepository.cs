using ListeDeCourses.Api.Models;
using MongoDB.Driver;

namespace ListeDeCourses.Api.Repositories;

public class IngredientRepository : BaseRepository<Ingredient>
{
    public IngredientRepository(IMongoDatabase database)
        : base(database, "ingredients") { }
}
