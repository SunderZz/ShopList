using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Models;

namespace ListeDeCourses.Api.Mappings;

public static class UtilisateurMappings
{
    public static UtilisateurReadDto ToReadDto(this Utilisateur u) => new()
    {
        Id          = u.Id ?? string.Empty,
        Email       = u.Email ?? string.Empty,
        Pseudo      = u.Pseudo ?? string.Empty,
        IsSuperUser = u.IsSuperUser
    };

    public static void Apply(this Utilisateur u, UtilisateurUpdateDto dto)
    {
        if (dto.Email is not null)      u.Email      = dto.Email;
        if (dto.Pseudo is not null)     u.Pseudo     = dto.Pseudo;
        if (dto.IsSuperUser.HasValue)   u.IsSuperUser = dto.IsSuperUser.Value;
    }
}
