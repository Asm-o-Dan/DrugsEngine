using Domain.Validators;
using Domain.Validators.Primitives;
using Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;

namespace Domain.Entities;

/// <summary>
/// Магазин лекарств
/// </summary>
public class DrugStore : BaseEntity<DrugStore>
{
    /// <summary>
    /// Конструктор сущности DrugStore
    /// </summary>
    /// <param name="address">Адрес магазина</param>
    /// <param name="number">Номер магазина</param>
    /// <param name="phoneNumber">Номер телефона магазина</param>
    /// <param name="drugNetwork">Сеть аптек, к которой относится магазин</param>
    /// <exception cref="ValidationException">Выбрасывается, если валидация не пройдена</exception>
    public DrugStore(Address address, int number, string phoneNumber, string drugNetwork)
    {
        if (!ExistingDrugStoreNumbers.DrugStoreNumbers.ContainsKey(drugNetwork))
        {
            ExistingDrugStoreNumbers.DrugStoreNumbers.Add(drugNetwork, new HashSet<int> { number });
        }
        else
        {
            ExistingDrugStoreNumbers.DrugStoreNumbers[drugNetwork].Add(number);
        }

        Address = address;
        Number = number;
        PhoneNumber = phoneNumber;
        DrugNetwork = drugNetwork;
    }
    public DrugStore(){}

    /// <summary>
    /// Адрес магазина
    /// </summary>
    public Address Address { get; set; }
    
    /// <summary>
    /// Номер магазина
    /// </summary>
    public int Number { get; set; }
    
    /// <summary>
    /// Номер телефона магазина
    /// </summary>
    public string PhoneNumber { get; set; }
    
    /// <summary>
    /// Сеть аптек, к которой относится магазин
    /// </summary>
    public string DrugNetwork { get; set; }
    
    /// <summary>
    /// Навигационное свойство для связи DrugStore и DrugItem
    /// </summary>
    public List<DrugItem> DrugItems { get; set; } = new List<DrugItem>();

}
