using Domain.Validators;
using FluentValidation;
using FluentValidation.Results;

namespace Domain.Entities;

/// <summary>
/// Лекарственный препарат
/// </summary>
public class Drug : BaseEntity<Drug>
{
    /// <summary>
    /// Конструктор сущности Drug
    /// </summary>
    /// <param name="name">Название препарата</param>
    /// <param name="manufacturer">Производитель</param>
    /// <param name="countryCodeId">Код страны производителя</param>
    /// <param name="country">Страна производитель</param>
    public Drug(string name, string manufacturer, string? countryCodeId, Country? country)
    {
        Name = name;
        Manufacturer = manufacturer;
        CountryCodeId = countryCodeId;
        Country = country;
        
        ValidateEntity(new DrugValidator());
    }
    
    public Drug(){}

    

    /// <summary>
    /// Название препарата
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// Производитель
    /// </summary>
    public string Manufacturer { get; private set; }
    
    /// <summary>
    /// Код страны производителя (ISO 3166-1 alpha-2); null — источник страну не указал
    /// </summary>
    public string? CountryCodeId { get; set; }
    
    public  Country? Country { get; set; }

    /// <summary>
    /// Форма выпуска («Таблетки», «Капсулы»…), если её удалось определить по названию
    /// </summary>
    public string? Form { get; private set; }

    /// <summary>
    /// Действующее вещество (МНН), если источник его указал
    /// </summary>
    public string? ActiveIngredient { get; private set; }

    /// <summary>
    /// Дополнить сведения о препарате. Уже известные значения не затираются пустыми.
    /// Возвращает true, если что-то изменилось.
    /// </summary>
    public bool SetDetails(string? form, string? activeIngredient)
    {
        var changed = false;
        if (!string.IsNullOrWhiteSpace(form) && Form != form) { Form = form; changed = true; }
        if (!string.IsNullOrWhiteSpace(activeIngredient) && ActiveIngredient != activeIngredient)
        {
            ActiveIngredient = activeIngredient;
            changed = true;
        }
        return changed;
    }
    /// <summary>
    /// Навигационное свойство для связи Drug и DrugItem
    /// </summary>
    public ICollection<DrugItem> DrugItems { get; set; } = new List<DrugItem>();
}