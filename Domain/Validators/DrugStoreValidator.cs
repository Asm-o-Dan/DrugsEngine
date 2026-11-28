using Domain.Entities;
using FluentValidation;

namespace Domain.Validators;

public class DrugStoreValidator : AbstractValidator<DrugStore>
{
    public DrugStoreValidator()
    {
        RuleFor(ds => ds.DrugNetwork)
            .NotNull().WithMessage(ValidationMessage.NotNull)
            .NotEmpty().WithMessage(ValidationMessage.NotEmpty)
            .Length(2, 100).WithMessage(ValidationMessage.WrongLength);

        RuleFor(ds => ds.Number)
            .GreaterThan(0).WithMessage(ValidationMessage.IntMustBeGreaterThanZero);

        RuleFor(ds => ds.PhoneNumber)
            .NotNull().WithMessage(ValidationMessage.NotNull)
            .NotEmpty().WithMessage(ValidationMessage.NotEmpty);

        RuleFor(ds => ds.Address)
            .NotNull().WithMessage(ValidationMessage.NotNull)
            .NotEmpty().WithMessage(ValidationMessage.NotEmpty);
    }
}