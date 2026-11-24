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
            .Length(2, 250).WithMessage(ValidationMessage.WrongLength)
            ;//.Matches(@"^[а-яА-Яa-zA-Z -]+\d+мг[а-яА-Яa-zA-Z -]+$").WithMessage(ValidationMessage.WrongMatchName);
        RuleFor(d => d.Manufacturer)
            .NotNull().WithMessage(ValidationMessage.NotNull)
            .NotEmpty().WithMessage(ValidationMessage.NotEmpty)
            .Length(1, 100).WithMessage(ValidationMessage.WrongLength)
            // буквы, цифры и пунктуация из реальных названий: «Сантал 2016», «Пик-фарма/корпорация Олифен», «Dr. Reddy's»
            .Matches(@"^[\p{L}\p{N} .,'&()/\\+\-""«»№]+$").WithMessage(ValidationMessage.OnlyLettersDigitsSpacesAndDashes);
        // Страна необязательна: источник указывает её не всегда.
        When(d => d.CountryCodeId != null, () =>
        {
            RuleFor(d => d.CountryCodeId!)
                .Matches(@"^[A-Z]{2}$").WithMessage(ValidationMessage.WrongMatchCountryCode)
                .Must(s => CountryCodes.AllCountryCodes.Contains(s)).WithMessage(ValidationMessage.CountryCodeInvalid);
        });
    }
}