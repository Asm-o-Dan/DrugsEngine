using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using FluentValidation;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Parsing;

/// <summary>
/// Парсер сети «Столетник» (100letnik.md). Список товаров — sitemap-shop.xml, по каждой карточке —
/// цены и уровень остатка во всех аптеках сети. Аптека определяется по телефону, часы работы — со страницы контактов.
/// Координат на сайте нет: аптеки геокодируются (Geocoding:Enabled).
/// </summary>
public class StoletnikParser(ILogger<StoletnikParser> logger, IOptions<ParsingOptions> options)
    : BaseParser(logger, options), IPharmacyParser
{
    public const string Network = "Столетник";
    private const string BaseUrl = "https://100letnik.md";
    private const int PostalCode = 3300;
    private static readonly TimeSpan ContactsCacheTtl = TimeSpan.FromHours(6);

    private IReadOnlyList<StoletnikContact>? _contacts;
    private DateTime _contactsLoadedAt;
    private readonly Dictionary<string, DrugStore> _stores = new();
    private readonly HashSet<string> _badStores = new();

    protected override TimeSpan RequestDelay => Options.Stoletnik.RequestDelay;

    public async Task<List<string>> ParseDrugsLinksAsync()
    {
        if (!Options.Stoletnik.Enabled)
        {
            logger.LogInformation("Парсер «Столетник» отключён (Parsing:Stoletnik:Enabled = false)");
            return [];
        }

        var response = await FetchPageContent($"{BaseUrl}/sitemap-shop.xml");
        if (response?.Content is null) return [];
        var urls = StoletnikSiteParser.ParseSitemap(response.Content).ToList();
        if (Options.Stoletnik.MaxProducts > 0) urls = urls.Take(Options.Stoletnik.MaxProducts).ToList();
        logger.LogInformation("«Столетник»: {Count} карточек товаров к обходу", urls.Count);
        return urls;
    }

    public async Task<List<DrugItem>?> ParseAsync(string url, CancellationToken cancellationToken = default)
    {
        var response = await FetchPageContent(url, cancellationToken);
        if (response?.Content is null) return null;

        var product = StoletnikSiteParser.ParseProduct(response.Content);
        if (product is null || product.Offers.Count == 0) return [];

        Drug drug;
        try
        {
            drug = new Drug(product.Name, product.Manufacturer, product.CountryCode,
                product.CountryCode is null ? null : new Country(product.CountryName!, product.CountryCode));
            drug.SetDetails(ProductNameAnalyzer.DetectForm(product.Name), ProductNameAnalyzer.DetectActiveIngredient(product.Name));
        }
        catch (ValidationException ex)
        {
            logger.LogDebug("Товар пропущен ({Name}): {Message}", product.Name, ex.Message);
            return [];
        }

        var contacts = await GetContactsAsync(cancellationToken);
        var items = new List<DrugItem>();
        foreach (var offer in product.Offers)
        {
            var store = GetStore(offer.Store, contacts);
            if (store is null) continue;
            // Точного количества сайт не показывает — только уровень остатка.
            items.Add(new DrugItem(1, offer.Price, drug, store, priceUpdatedAt: null, stockLevel: offer.Level));
        }
        return items;
    }

    private DrugStore? GetStore(StoletnikStoreInfo info, IReadOnlyList<StoletnikContact> contacts)
    {
        if (_stores.TryGetValue(info.Key, out var cached)) return cached;
        if (_badStores.Contains(info.Key)) return null;

        try
        {
            var hours = contacts.FirstOrDefault(c => c.PhoneKey == info.Key)?.Hours;
            var phone = info.Phone.StartsWith("0 ") ? info.Phone[2..] : info.Phone;
            var store = new DrugStore(new Address(info.City, info.Street, info.House, PostalCode), 0, phone, Network);
            store.SetExternalKey(info.Key);
            store.UpdateDetails(store.Address, phone, hours, null, null);
            _stores[info.Key] = store;
            return store;
        }
        catch (ValidationException ex)
        {
            _badStores.Add(info.Key);
            logger.LogWarning("Аптека пропущена ({City}, {Street} {House}): {Message}", info.City, info.Street, info.House, ex.Message);
            return null;
        }
    }

    private async Task<IReadOnlyList<StoletnikContact>> GetContactsAsync(CancellationToken ct)
    {
        if (_contacts is not null && DateTime.UtcNow - _contactsLoadedAt < ContactsCacheTtl)
            return _contacts;

        var response = await FetchPageContent($"{BaseUrl}/kontakty/", ct);
        _contacts = response?.Content is null ? [] : StoletnikSiteParser.ParseContacts(response.Content);
        _contactsLoadedAt = DateTime.UtcNow;
        _stores.Clear(); // часы работы могли измениться
        logger.LogInformation("«Столетник», страница контактов: {Count} аптек", _contacts.Count);
        return _contacts;
    }
}
