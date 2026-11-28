using FluentValidation;

namespace Domain.ValueObjects;

/// <summary>
/// Базовый класс для всех объектов значений, обеспечивающий эффективное сравнение и вычисление хеш-кода без использования Reflection.
/// </summary>
public abstract class BaseValueObject : IEquatable<BaseValueObject>
{
    /// <summary>
    /// Возвращает компоненты объекта значения, участвующие в проверке равенства.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Определяет, равен ли текущий объект значению другого объекта.
    /// </summary>
    /// <param name="other">Другой объект для сравнения.</param>
    /// <returns>True, если объекты равны; иначе False.</returns>
    public bool Equals(BaseValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Переопределение метода Equals для сравнения объектов.
    /// </summary>
    /// <param name="obj">Объект для сравнения.</param>
    /// <returns>True, если объекты равны; иначе False.</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as BaseValueObject);
    }

    /// <summary>
    /// Переопределение метода GetHashCode для вычисления хеш-кода на основе компонентов объекта.
    /// </summary>
    /// <returns>Хеш-код объекта.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Оператор сравнения на равенство для объектов значений.
    /// </summary>
    /// <param name="left">Левый объект для сравнения.</param>
    /// <param name="right">Правый объект для сравнения.</param>
    /// <returns>True, если объекты равны; иначе False.</returns>
    public static bool operator ==(BaseValueObject? left, BaseValueObject? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>
    /// Оператор сравнения на неравенство для объектов значений.
    /// </summary>
    /// <param name="left">Левый объект для сравнения.</param>
    /// <param name="right">Правый объект для сравнения.</param>
    /// <returns>True, если объекты не равны; иначе False.</returns>
    public static bool operator !=(BaseValueObject? left, BaseValueObject? right)
    {
        return !(left == right);
    }
        
    /// <summary>
    /// Метод для выполнения валидации объекта значения с использованием указанного валидатора.
    /// </summary>
    /// <typeparam name="T">Тип объекта значения.</typeparam>
    /// <param name="validator">Валидатор для проверки.</param>
    /// <exception cref="ValidationException">Исключение, если валидация не удалась.</exception>
    protected void ValidateValueObject<T>(IValidator<T> validator) where T : BaseValueObject
    {
        var result = validator.Validate((T)this);
        if (!result.IsValid)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException($"Validation failed for {typeof(T).Name}: {errors}");
        }
    }
}