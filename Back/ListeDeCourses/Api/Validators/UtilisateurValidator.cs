using FluentValidation;
using ListeDeCourses.Api.DTOs;
using static ListeDeCourses.Api.Validators.ValidationHelpers;

namespace ListeDeCourses.Api.Validators;

public class UtilisateurCreateDtoValidator : AbstractValidator<UtilisateurCreateDto>
{
    public UtilisateurCreateDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(EmailMax);

        RuleFor(x => x.Pseudo)
            .NotEmpty()
            .Length(PseudoMin, PseudoMax);

        RuleFor(x => x.Password)
            .NotEmpty()
            .Length(PasswordMin, PasswordMax);
    }
}

public class UtilisateurUpdateDtoValidator : AbstractValidator<UtilisateurUpdateDto>
{
    public UtilisateurUpdateDtoValidator()
    {
        When(x => x.Email is not null, () =>
        {
            RuleFor(x => x.Email!)
                .EmailAddress()
                .MaximumLength(EmailMax);
        });

        When(x => x.Pseudo is not null, () =>
        {
            RuleFor(x => x.Pseudo!)
                .Length(PseudoMin, PseudoMax);
        });

        When(x => x.Password is not null, () =>
        {
            RuleFor(x => x.Password!)
                .Length(PasswordMin, PasswordMax);
        });
    }
}
