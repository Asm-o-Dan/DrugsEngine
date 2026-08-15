using System.Data;
using Domain.Entities;
using FluentValidation;

namespace Domain.Validators;

public class CountryValidator : AbstractValidator<Country>
{
    public CountryValidator()
    {
        RuleFor(c => c.Name)
            .NotNull().WithMessage(ValidationMessage.NotNull)
            .NotEmpty().WithMessage(ValidationMessage.NotEmpty)
            .Length(2, 100).WithMessage(ValidationMessage.WrongLength)
            .Matches(@"^[а-яА-ЯёЁa-zA-Z\s\-.]+$").WithMessage(ValidationMessage.WrongMatchName);
        RuleFor(c => c.Code)
            .Length(2).WithMessage(ValidationMessage.WrongExactLength)
            .Matches(@"^[A-Z]{2}$").WithMessage(ValidationMessage.CountryCodeInvalid)
            .Must(s => CountryCodes.AllCountryCodes.Contains(s)).WithMessage(ValidationMessage.CountryCodeInvalid);
    }
}
