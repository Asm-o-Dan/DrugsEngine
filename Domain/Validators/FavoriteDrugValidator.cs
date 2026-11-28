using Domain.Entities;
using FluentValidation;

namespace Domain.Validators;

public class FavoriteDrugValidator : AbstractValidator<FavoriteDrug>
{
    public FavoriteDrugValidator()
    {
        RuleFor(fd => fd.ProfileId)
            .NotEmpty()
            .WithMessage("Идентификатор профиля (ProfileId) не может быть пустым.");

        RuleFor(fd => fd.DrugId)
            .NotEmpty()
            .WithMessage("Идентификатор лекарства (DrugId) не может быть пустым.");

        RuleFor(fd => fd.ExternalUserId)
            .NotEmpty()
            .WithMessage("Идентификатор пользователя во внешней системе не может быть пустым.");

        RuleFor(fd => fd.Drug)
            .NotNull()
            .WithMessage("Сущность лекарства (Drug) обязательна.");
    }
}