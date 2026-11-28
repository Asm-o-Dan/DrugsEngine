using Domain.Validators;

namespace Domain.ValueObjects;

public class Email: BaseValueObject
{
    /// <summary>
    /// Конструктор электронной почты
    /// </summary>
    /// <param name="value">Электронная почта.</param>
    public Email(string value)
    {
        Value = value;

        // Вызов валидации для электронной почты.
        ValidateValueObject(new EmailValidator());
    }

    /// <summary>
    /// Значение эл. почты
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Возвращает строковое представление адреса.
    /// </summary>
    /// <returns>Строка, представляющая адрес.</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}