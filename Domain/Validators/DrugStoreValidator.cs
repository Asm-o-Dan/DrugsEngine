using Domain.Entities;
using Domain.Validators.Primitives;
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
            .GreaterThan(0).WithMessage(ValidationMessage.IntMustBeGreaterThanZero)
            .Must((ds ,n) => ExistingDrugStoreNumbers.DrugStoreNumbers.ContainsKey(ds.DrugNetwork))
            .WithMessage(ValidationMessage.InexesistingNetwork)
            .Must((ds, n) => ExistingDrugStoreNumbers.DrugStoreNumbers.ContainsKey(ds.DrugNetwork) && !ExistingDrugStoreNumbers.DrugStoreNumbers[ds.DrugNetwork].Contains(ds.Number))
            .WithErrorCode("100").WithMessage(ValidationMessage.AlreadyExist);

        RuleFor(ds => ds.Address)
            .NotNull().WithMessage(ValidationMessage.NotNull)
            .NotEmpty().WithMessage(ValidationMessage.NotEmpty);

    }
}