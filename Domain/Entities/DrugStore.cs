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
    /// Устойчивый идентификатор аптеки у источника — цифры телефона.
    /// Номеров у аптек на сайте сети нет, а телефон сайт сам использует, чтобы указать аптеку в прайсе.
    /// </summary>
    public string? ExternalKey { get; private set; }

    /// <summary>
    /// Часы работы в формате источника, например «8:00–20:00»
    /// </summary>
    public string? Hours { get; private set; }

    /// <summary>
    /// Широта (WGS 84); null — адрес ещё не геокодирован или не найден
    /// </summary>
    public double? Latitude { get; private set; }

    /// <summary>
    /// Долгота (WGS 84)
    /// </summary>
    public double? Longitude { get; private set; }

    /// <summary>
    /// Когда последний раз пытались геокодировать адрес (UTC)
    /// </summary>
    public DateTime? GeocodedAt { get; private set; }

    /// <summary>
    /// Ключ аптеки по телефону: только цифры, без ведущих нулей («0 (533) 6-66-00» → «53366600»).
    /// </summary>
    public static string? PhoneKey(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = new string(phone.Where(char.IsDigit).ToArray()).TrimStart('0');
        return digits.Length >= 5 ? digits : null;
    }

    public void SetExternalKey(string externalKey)
    {
        if (string.IsNullOrWhiteSpace(externalKey)) throw new ArgumentException("Пустой ключ аптеки", nameof(externalKey));
        ExternalKey = externalKey;
    }

    /// <summary>
    /// Внутренний номер аптеки в сети. Назначается один раз при создании и больше не меняется.
    /// </summary>
    public void AssignNumber(int number)
    {
        if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number));
        Number = number;
    }

    /// <summary>
    /// Обновить данные аптеки из источника. Возвращает true, если что-то изменилось.
    /// Смена адреса без новых координат сбрасывает старые: они относились к прежнему адресу.
    /// </summary>
    public bool UpdateDetails(Address address, string phoneNumber, string? hours, double? latitude, double? longitude)
    {
        var changed = false;
        var addressChanged = !Equals(Address, address);
        if (addressChanged)
        {
            Address = address;
            changed = true;
        }
        if (PhoneNumber != phoneNumber) { PhoneNumber = phoneNumber; changed = true; }
        if (hours != null && Hours != hours) { Hours = hours; changed = true; }

        if (latitude is not null && longitude is not null)
        {
            if (Latitude != latitude || Longitude != longitude) { SetLocation(latitude, longitude); changed = true; }
        }
        else if (addressChanged)
        {
            Latitude = Longitude = null;
            GeocodedAt = null;
        }
        return changed;
    }

    /// <summary>
    /// Сохранить результат геокодирования; null — адрес не найден.
    /// </summary>
    public void SetLocation(double? latitude, double? longitude)
    {
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
        Latitude = latitude;
        Longitude = longitude;
        GeocodedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Навигационное свойство для связи DrugStore и DrugItem
    /// </summary>
    public List<DrugItem> DrugItems { get; set; } = new List<DrugItem>();

}
