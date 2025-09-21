using System.ComponentModel.DataAnnotations;

namespace ListeDeCourses.Api.DTOs
{
    public record UtilisateurReadDto
    {
        public required string Id { get; init; }

        [Required, EmailAddress, MaxLength(DtoConstraints.EmailMax)]
        public required string Email { get; init; }

        [Required, MinLength(DtoConstraints.PseudoMin), MaxLength(DtoConstraints.PseudoMax)]
        public required string Pseudo { get; init; }

        public required bool IsSuperUser { get; init; }
    }

    public record UtilisateurCreateDto
    {
        [Required, EmailAddress, MaxLength(DtoConstraints.EmailMax)]
        public required string Email { get; init; }

        [Required, MinLength(DtoConstraints.PseudoMin), MaxLength(DtoConstraints.PseudoMax)]
        public required string Pseudo { get; init; }

        [Required, MinLength(DtoConstraints.PasswordMin), MaxLength(DtoConstraints.PasswordMax)]
        public required string Password { get; init; }

        public bool? IsSuperUser { get; init; }
    }

    public record UtilisateurUpdateDto
    {
        [EmailAddress, MaxLength(DtoConstraints.EmailMax)]
        public string? Email { get; init; }

        [MinLength(DtoConstraints.PseudoMin), MaxLength(DtoConstraints.PseudoMax)]
        public string? Pseudo { get; init; }

        [MinLength(DtoConstraints.PasswordMin), MaxLength(DtoConstraints.PasswordMax)]
        public string? Password { get; init; }

        public bool? IsSuperUser { get; init; }
    }
}
