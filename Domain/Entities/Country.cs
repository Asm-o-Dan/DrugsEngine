using System;
using FluentValidation.Results;
using Domain.Validators;
using FluentValidation;

namespace Domain.Entities;

/// <summary>
/// Страна
/// </summary>
public class Country : BaseEntity<Country>
{
    /// <summary>
    /// Конструктор сущности Country
    /// </summary>
    /// <param name="name">Название страны</param>
    /// <param name="code">Код страны по формату ISO3166-1 alpha-2</param>
    /// <exception cref="ValidationException">Выбрасывается, если валидация не пройдена</exception>
    public Country(string name, string code)
    {
        Name = name;
        Code = code;

        // Валидация при создании объекта
        ValidateEntity(new CountryValidator());
    }
    public Country(){}

    /// <summary>
    /// Название страны
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Код страны
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Коллекция препаратов, производимых в этой стране
    /// </summary>
    public ICollection<Drug> Drugs { get; private set; } = new List<Drug>();
}