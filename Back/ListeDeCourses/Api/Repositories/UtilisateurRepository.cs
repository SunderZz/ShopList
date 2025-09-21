using ListeDeCourses.Api.Models;
using MongoDB.Driver;

namespace ListeDeCourses.Api.Repositories;

public class UtilisateurRepository : BaseRepository<Utilisateur>
{
    public UtilisateurRepository(IMongoDatabase database)
        : base(database, "utilisateurs") { }

    public async Task<Utilisateur?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _collection.Find(x => x.Email == email).FirstOrDefaultAsync(ct);
}
