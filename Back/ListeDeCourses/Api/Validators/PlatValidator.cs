using FluentValidation;
using ListeDeCourses.Api.DTOs;
using static ListeDeCourses.Api.Validators.ValidationHelpers;

namespace ListeDeCourses.Api.Validators;

public class PlatIngredientDtoValidator : AbstractValidator<PlatIngredientDto>
{
    public PlatIngredientDtoValidator()
    {
        RuleFor(x => x.IngredientId)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).When(x => x.Quantity.HasValue);

        RuleFor(x => x.Unit)
            .MaximumLength(UnitMax).When(x => x.Unit is not null);
    }
}

public class PlatCreateDtoValidator : AbstractValidator<PlatCreateDto>
{
    public PlatCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(NameMin, NameMax);

        RuleFor(x => x.Ingredients)
            .NotNull()
            .NotEmpty();

        RuleForEach(x => x.Ingredients)
            .SetValidator(new PlatIngredientDtoValidator());
    }
}

public class PlatUpdateDtoValidator : AbstractValidator<PlatUpdateDto>
{
    public PlatUpdateDtoValidator()
    {
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name!)
                .Length(NameMin, NameMax);
        });

        When(x => x.Ingredients is not null, () =>
        {
            RuleForEach(x => x.Ingredients!)
                .SetValidator(new PlatIngredientDtoValidator());
        });
    }
}
