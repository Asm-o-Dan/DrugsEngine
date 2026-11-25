using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using FluentValidation;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Parsing;

/// <summary>
/// Парсер сети «Доктор» (aptekadoktor.com).
/// Аптеки берутся прямо из прайса (адрес, телефон, координаты), часы работы — со страницы контактов.
/// Идентификатор аптеки — её телефон: номеров у аптек на сайте нет.
/// </summary>
public class DoctorParser(ILogger<DoctorParser> logger, IOptions<ParsingOptions> options)
    : BaseParser(logger, options), IPharmacyParser
{
    public const string Network = "Доктор";
    private const string BaseUrl = "https://www.aptekadoktor.com";
    private const int PostalCode = 3300;
    private static readonly TimeSpan ContactsCacheTtl = TimeSpan.FromHours(6);

    private IReadOnlyList<DoctorContactInfo>? _contacts;
    private DateTime _contactsLoadedAt;
    private readonly Dictionary<string, DrugStore> _stores = new();
    private readonly HashSet<string> _unknownCountries = new();

    public async Task<List<DrugItem>?> ParseAsync(string url, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Парсинг страницы: {Url}", url);
        var response = await FetchPageContent(url, cancellationToken);
        if (response?.Content is null) return null;

        var contacts = await GetContactsAsync(cancellationToken);
        var skipped = 0;
        var rows = DoctorSiteParser.ParseAvailability(response.Content, _ => skipped++);

        var drugs = new Dictionary<string, Drug>();
        var items = new List<DrugItem>();
        foreach (var row in rows)
        {
            try
            {
                var store = GetStore(row.Store, contacts);
                var drugKey = $"{row.Name}|{row.Manufacturer}";
                if (!drugs.TryGetValue(drugKey, out var drug))
                {
                    if (row.CountryCode is null && _unknownCountries.Add(row.Manufacturer))
                        logger.LogDebug("Страна производителя не определена: {Manufacturer}", row.Manufacturer);
                    drug = new Drug(row.Name, row.Manufacturer, row.CountryCode,
                        row.CountryCode is null ? null : new Country(row.CountryName!, row.CountryCode));
                    drug.SetDetails(ProductNameAnalyzer.DetectForm(row.Name), ProductNameAnalyzer.DetectActiveIngredient(row.Name));
                    drug.SetMatchKey(ProductKey.Compute(row.Name, row.Manufacturer, drug.Form));
                    drugs[drugKey] = drug;
                }
                // Реального остатка сайт не показывает — только факт наличия.
                items.Add(new DrugItem(1, row.Price, drug, store, row.UpdatedAtUtc));
            }
            catch (ValidationException ex)
            {
                skipped++;
                logger.LogDebug("Строка пропущена ({Name}): {Message}", row.Name, ex.Message);
            }
        }

        if (skipped > 0)
            logger.LogWarning("{Url}: пропущено {Skipped} из {Total} строк (не удалось разобрать или не прошли валидацию)",
                url, skipped, rows.Count + skipped);
        return items;
    }

    public Task<List<string>> ParseDrugsLinksAsync()
    {
        // Прайс фильтруется по первой букве названия.
        var letters = Enumerable.Range('А', 'Я' - 'А' + 1).Select(c => ((char)c).ToString())
            .Concat(["0-9", "A-Z"]);
        return Task.FromResult(letters
            .Select(k => $"{BaseUrl}/availability/?keyword={Uri.EscapeDataString(k)}&city_id=")
            .ToList());
    }

    private DrugStore GetStore(DoctorStoreInfo info, IReadOnlyList<DoctorContactInfo> contacts)
    {
        if (_stores.TryGetValue(info.Key, out var cached)) return cached;

        var addressKey = DoctorSiteParser.AddressKey($"{info.Street} {info.House}");
        var contact = contacts.FirstOrDefault(c => c.PhoneKey == info.Key)
                      ?? contacts.FirstOrDefault(c => c.AddressKey == addressKey);
        if (contact is null)
            logger.LogInformation("Аптека {Street} {House} не найдена на странице контактов — без часов работы", info.Street, info.House);

        var store = new DrugStore(new Address(info.City, info.Street, info.House, PostalCode), 0, info.Phone, Network);
        store.SetExternalKey(info.Key);
        store.UpdateDetails(store.Address, info.Phone, contact?.Hours, info.Latitude, info.Longitude);
        _stores[info.Key] = store;
        return store;
    }

    private async Task<IReadOnlyList<DoctorContactInfo>> GetContactsAsync(CancellationToken ct)
    {
        if (_contacts is not null && DateTime.UtcNow - _contactsLoadedAt < ContactsCacheTtl)
            return _contacts;

        var response = await FetchPageContent($"{BaseUrl}/contact", ct);
        _contacts = response?.Content is null ? [] : DoctorSiteParser.ParseContacts(response.Content);
        _contactsLoadedAt = DateTime.UtcNow;
        _stores.Clear(); // часы работы могли измениться
        logger.LogInformation("Страница контактов: {Count} аптек", _contacts.Count);
        return _contacts;
    }
}
