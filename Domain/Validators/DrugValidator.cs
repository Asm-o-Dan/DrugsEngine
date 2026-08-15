using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Domain.Entities;
using FluentValidation;

namespace Domain.Validators;

public class DrugValidator : AbstractValidator<Drug>
{
    public DrugValidator()
    {
        RuleFor(d => d.Name)
            .NotNull().WithMessage(ValidationMessage.NotNull)
            .NotEmpty().WithMessage(ValidationMessage.NotEmpty)
            .Length(2, 150).WithMessage(ValidationMessage.WrongLength)
            ;//.Matches(@"^[а-яА-Яa-zA-Z -]+\d+мг[а-яА-Яa-zA-Z -]+$").WithMessage(ValidationMessage.WrongMatchName);
        RuleFor(d => d.Manufacturer)
            .NotNull().WithMessage(ValidationMessage.NotNull)
            .NotEmpty().WithMessage(ValidationMessage.NotEmpty)
            .Matches(@"^[а-яА-ЯёЁa-zA-Z0-9\s\-.,()""'«»]+$").WithMessage(ValidationMessage.OnlyLettersDigitsSpacesAndDashes);
        RuleFor(d => d.CountryCodeId)
            .Matches(@"^[A-Z]{2}$").WithMessage(ValidationMessage.WrongMatchCountryCode)
            .Must(s => CountryCodes.AllCountryCodes.Contains(s)).WithMessage(ValidationMessage.CountryCodeInvalid);
    }
}
