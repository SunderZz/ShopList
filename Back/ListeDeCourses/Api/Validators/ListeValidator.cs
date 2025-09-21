using FluentValidation;
using ListeDeCourses.Api.DTOs;
using static ListeDeCourses.Api.Validators.ValidationHelpers;

namespace ListeDeCourses.Api.Validators;

public class ListeItemDtoValidator : AbstractValidator<ListeItemDto>
{
    public ListeItemDtoValidator()
    {
        RuleFor(x => x.IngredientId)
            .NotEmpty();

        RuleFor(x => x.IngredientName)
            .NotEmpty()
            .Length(NameMin, IngredientNameMax);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).When(x => x.Quantity.HasValue);

        RuleFor(x => x.Unit)
            .MaximumLength(UnitMax).When(x => x.Unit is not null);

        RuleFor(x => x.Aisle)
            .MaximumLength(AisleMax).When(x => x.Aisle is not null);
    }
}

public class ListeCreateDtoValidator : AbstractValidator<ListeCreateDto>
{
    public ListeCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(NameMin, NameMax);

        When(x => x.Items is not null, () =>
        {
            RuleForEach(x => x.Items!)
                .SetValidator(new ListeItemDtoValidator());
        });
    }
}

public class ListeUpdateDtoValidator : AbstractValidator<ListeUpdateDto>
{
    public ListeUpdateDtoValidator()
    {
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name!)
                .Length(NameMin, NameMax);
        });

        When(x => x.Items is not null, () =>
        {
            RuleForEach(x => x.Items!)
                .SetValidator(new ListeItemDtoValidator());
        });
    }
}
