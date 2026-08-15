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
    public Drug(string name, string manufacturer, string countryCodeId, Country? country)
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
    /// ID Страны
    /// </summary>
    public string CountryCodeId { get; set; }
    
    public  Country? Country { get; set; }
    /// <summary>
    /// Навигационное свойство для связи Drug и DrugItem
    /// </summary>
    public ICollection<DrugItem> DrugItems { get; set; } = new List<DrugItem>();
}