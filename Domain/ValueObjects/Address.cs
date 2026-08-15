using Domain.Validators;

namespace Domain.ValueObjects;

/// <summary>
/// Полный адресс
/// </summary>
public class Address : BaseValueObject
{
    /// <summary>
    /// Конструктор класса Address
    /// </summary>
    /// <param name="city">Город</param>
    /// <param name="street">Улица</param>
    /// <param name="house">Номер дома</param>
    /// <param name="postalCode">Почтовый индекс</param>
    public Address(string city, string street, string house,int postalCode) 
    {
        City = city;
        Street = street ;
        House = house == "" ? "1" : house;
        PostalCode = postalCode;
        
        ValidateValueObject(new AddressValidator());
    }

    /// <summary>
    /// Город
    /// </summary>
    public string City { get; private set; }
    /// <summary>
    /// Улица
    /// </summary>
    public string Street { get; private set; }
    /// <summary>
    /// Номер дома
    /// </summary>
    public string House { get; private set; }
    /// <summary>
    /// Почтовый индекс
    /// </summary>
    public int PostalCode { get; private set; }
}