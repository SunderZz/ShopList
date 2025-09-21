using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Mappings;
using ListeDeCourses.Api.Models;
using ListeDeCourses.Api.Repositories;

namespace ListeDeCourses.Api.Services;

public class UtilisateurService : BaseService<UtilisateurReadDto, UtilisateurCreateDto, UtilisateurUpdateDto, Utilisateur>
{
    public UtilisateurService(UtilisateurRepository repository) : base(repository) { }

    protected override UtilisateurReadDto MapToReadDto(Utilisateur entity) => entity.ToReadDto();

    protected override Utilisateur MapToEntity(UtilisateurCreateDto dto) => new()
    {
        Email = dto.Email,
        Pseudo = dto.Pseudo,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        IsSuperUser = dto.IsSuperUser ?? false
    };

    protected override void ApplyUpdate(Utilisateur entity, UtilisateurUpdateDto dto) => entity.Apply(dto);

    public async Task<UtilisateurReadDto?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await ((UtilisateurRepository)_repository).GetByEmailAsync(email, ct);
        return user is null ? null : MapToReadDto(user);
    }
}
