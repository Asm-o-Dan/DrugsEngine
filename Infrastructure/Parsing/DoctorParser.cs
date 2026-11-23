using System.Globalization;
using System.Text.RegularExpressions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Validators;
using Domain.ValueObjects;
using FluentValidation;
using HtmlAgilityPack;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Parsing;

/// <summary>
/// Парсер для получения информации о лекарствах и аптеках.
/// </summary>
public class DoctorParser : BaseParser, IPharmacyParser
{
    
    private static readonly TimeSpan StoresCacheTtl = TimeSpan.FromHours(1);
    private readonly ILogger<DoctorParser> _logger;
    private List<DrugStore>? _storesCache;
    private DateTime _storesCachedAt;

    public DoctorParser(ILogger<DoctorParser> logger, IOptions<ParsingOptions> options) : base(logger, options)
    {
        _logger = logger;
    }

    /// <summary>Список аптек нужен для каждой страницы, но меняется редко — кешируем.</summary>
    private async Task<List<DrugStore>> GetDrugStoresAsync(CancellationToken ct)
    {
        if (_storesCache is { Count: > 0 } && DateTime.UtcNow - _storesCachedAt < StoresCacheTtl)
            return _storesCache;
        _storesCache = await ParseDrugStoresAsync(ct) ?? [];
        _storesCachedAt = DateTime.UtcNow;
        return _storesCache;
    }

    /// <summary>
    /// Парсит страницу конкретного лекарства и получает данные о нём.
    /// </summary>
    /// <param name="url">URL страницы с информацией о лекарстве.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список объектов <see cref="DrugItem"/> или null.</returns>
     public async Task<List<DrugItem>?> ParseAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Парсинг страницы: {url}");
        try
        {
            var doc = await FetchHtmlDocument(url, cancellationToken);
            var rows = doc?.DocumentNode.SelectNodes("//tr")?.Skip(1).ToList();
            if (rows == null)
            {
                _logger.LogWarning("Тег <tbody> или <tr> не найден.");
                return null;
            }

            var drugItems = new List<DrugItem>();
            var drugCache = new Dictionary<string, Drug>();
            var drugStores = await GetDrugStoresAsync(cancellationToken);
            
            foreach (var row in rows)
            {
                var cells = row.SelectNodes("./td");
                if (cells == null) continue;

                var drugName = Regex.Replace(cells[0].InnerText.Trim(), @"(?:№\d+\s*)|\([^)]*\)", "").Trim();
                string countryName, drugManufacturer;
                var manufacturerParts = cells[1].InnerText.Split(",");
                
                if (manufacturerParts.Length == 2)
                {
                    drugManufacturer = manufacturerParts[0].Trim();
                    countryName = manufacturerParts[1].Trim();
                }
                else
                {
                    drugManufacturer = manufacturerParts[0].Trim();
                    countryName = manufacturerParts[0].Split(" ").Last().Trim();
                }

                var countryCode = CountryCodes.GetCodeByRussianName(countryName);
                if (string.IsNullOrEmpty(countryCode))
                {
                    //_logger.LogWarning($"Ошибка: неизвестный код страны для {countryName}");
                    countryCode = "RU";
                    countryName = "Россия";
                }
                
                string drugKey = $"{drugName}|{drugManufacturer}|{countryCode}";
                if (!drugCache.TryGetValue(drugKey, out var drug))
                {
                    try
                    {
                        drug = new Drug(drugName, drugManufacturer, countryCode, new Country(countryName, countryCode));
                        drugCache[drugKey] = drug;
                    }
                    catch (ValidationException ex)
                    {
                        _logger.LogError($"Ошибка валидации: {ex.Message}");
                        continue;
                    }
                }
                
                if (!decimal.TryParse(cells[3].InnerText.Replace("руб.", "").Replace(" ", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                {
                    _logger.LogWarning($"Ошибка парсинга цены: {cells[3].InnerText}");
                    continue;
                }
                
                var phoneData = cells[4].InnerText.Split(",");
                string phoneNumber = phoneData.Length >= 3 ? phoneData.Last().Replace(".: 0", "").Trim() : "";
                var drugStore = drugStores.FirstOrDefault(x => x.PhoneNumber.Trim() == phoneNumber);
                
                if (drugStore == null)
                {
                    _logger.LogWarning($"Аптека с номером {phoneNumber} не найдена.");
                    continue;
                }

                var drugItem = new DrugItem(1, price, drug, drugStore);
                drugItems.Add(drugItem);
            }
            return drugItems;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при парсинге URL: {Url}", url);
            return null;
        }
    }
    /// <summary>
    /// Парсит список аптек с сайта и заполняет <see cref="DrugStores"/>.
    /// </summary>
    public async Task<List<DrugStore>?> ParseDrugStoresAsync(CancellationToken ct = default)
    {
        //_logger.LogInformation("Парсинг списка аптек...");
        var url = "https://www.aptekadoktor.com/contact";
        var response = await FetchPageContent(url, ct);
        if (response?.Content == null) return null;

        var doc = new HtmlDocument();
        doc.LoadHtml(response.Content);
        var nodes = doc.DocumentNode.SelectNodes("//span[text()[contains(.,'тел')]]")?.ToList() ?? [];
        var drugStores = new List<DrugStore>();

        for (int i = 1; i <= nodes.Count; i++)
        {
            var htmlNode = nodes[i - 1];
            try
            {
                var storeData = htmlNode.InnerText.Trim().Split(",").Skip(1).ToList();
                var city = storeData[0].Contains("Днестровск") ? "Днестровск" : "Тирасполь";
                var street = storeData[0].Replace("г. Днестровск ", "").Trim();
                string house;
                string phoneNumber;
                if (storeData.Count == 3)
                {
                    house = storeData[0].Split(" ").Last().Replace("\"","").Trim();
                    phoneNumber = storeData[0].Contains("Энергетиков") ? "тел (219) 7-12-42" :storeData[1].Replace(".","").Insert(10," ").Trim();
                }
                else if (storeData.Count == 4)
                {
                    house = storeData[1].Replace("\"","").Trim();
                    phoneNumber = storeData[2].Replace(".","").Trim();
                }
                else
                {
                    house = "24";
                    phoneNumber = "тел (219) 3-06-66";
                } var storeAddress = new Address(city, street, house, 33000);
                var drugStore = new DrugStore(storeAddress, i, phoneNumber, "Доктор");
                drugStores.Add(drugStore);
                
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка обработки аптеки: {ex.Message}");
            }
        }
        return drugStores;
    }
    
    public async Task<List<string>> ParseDrugsLinksAsync()
    {
        _logger.LogInformation("Начинаем парсинг ссылок на лекарства...");
    
        var letters = Enumerable.Range('А', 'Я' - 'А' + 1).Select(c => (char)c);
        var links = letters.Select(c => $"https://www.aptekadoktor.com/availability/?keyword={c}&city_id=").ToList();
    
        links.AddRange(new[] { "0-9", "A-Z" }.Select(c => $"https://www.aptekadoktor.com/availability/?keyword={c}&city_id="));

        _logger.LogInformation($"Всего найдено {links.Count} товаров.");
        return links;
    }
}
