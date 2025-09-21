using FluentValidation;
using ListeDeCourses.Api.DTOs;
using static ListeDeCourses.Api.Validators.ValidationHelpers;

namespace ListeDeCourses.Api.Validators;

public class IngredientCreateDtoValidator : AbstractValidator<IngredientCreateDto>
{
    public IngredientCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(NameMin, IngredientNameMax);

        RuleFor(x => x.Aisle)
            .MaximumLength(AisleMax)
            .When(x => x.Aisle is not null);
    }
}

public class IngredientUpdateDtoValidator : AbstractValidator<IngredientUpdateDto>
{
    public IngredientUpdateDtoValidator()
    {
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name!)
                .Length(NameMin, IngredientNameMax);
        });

        When(x => x.Aisle is not null, () =>
        {
            RuleFor(x => x.Aisle!)
                .MaximumLength(AisleMax);
        });
    }
}
