using System.Globalization;
using System.Text.RegularExpressions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Validators;
using Domain.ValueObjects;
using HtmlAgilityPack;
using RestSharp;

namespace Infrastructure.Parsing;

public class VivaFarmParser(ILogger<VivaFarmParser> logger) : BaseParser(logger)//, IPharmacyParser
{
    /// <summary>
    /// Извлекает название лекарства, удаляя ненужные элементы.
    /// </summary>
    private string ParseDrugName(HtmlDocument doc)
    {
        var node = doc.DocumentNode.SelectSingleNode("//h1[@id='pagetitle']");
        return node != null
            ? Regex.Replace(node.InnerText.Trim(), @"(?:№\d+\s*)|\([^)]*\)", "").Trim()
            : "Неизвестное лекарство";
    }

    /// <summary>
    /// Извлекает производителя и страну происхождения лекарства.
    /// </summary>
    private (string drugManufacturer, string countryName) ParseDrugProperties(HtmlDocument doc)
    {
        string drugManufacturer = "";
        string countryName = "";

        var properties = doc.DocumentNode.SelectNodes("//div[contains(@class, 'properties__item')]");
        if (properties != null)
        {
            foreach (var item in properties)
            {
                var titleNode = item.SelectSingleNode(".//div[contains(@class, 'properties__title')]");
                var valueNode = item.SelectSingleNode(".//div[contains(@class, 'properties__value')]");

                if (titleNode != null && valueNode != null)
                {
                    string title = titleNode.InnerText.Trim();
                    string value = valueNode.InnerText.Trim();

                    if (title == "Бренд") drugManufacturer = value;
                    else if (title == "Страна происхождения") countryName = value;
                }
            }
        }

        return (drugManufacturer, countryName);
    }

    /// <summary>
    /// Парсит страницу конкретного лекарства и получает данные о нём.
    /// </summary>
    public async Task<List<DrugItem>?> ParseAsync(string url, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Парсинг страницы: {Url}", url);

        var response = await FetchPageContent(url, cancellationToken: cancellationToken);
        if (response == null) return null;

        var doc = new HtmlDocument();
        doc.LoadHtml(response.Content);

        //Получаем название лекарства
        string drugName = ParseDrugName(doc);

        // Получаем производителя и страну происхождения
        (string drugManufacturer, string countryName) = ParseDrugProperties(doc);
        string countryCode = CountryCodes.GetCodeByRussianName(countryName);
        // Создаём объект лекарства
        var drug = new Drug(
            drugName,
            drugManufacturer,
            countryCode,
            new Country(countryName, countryCode)
        );

        // Получаем ID лекарства для запроса к API наличия
        string dataItem = GetDrugDataItem(doc);
        if (dataItem == "не найдено") return null;

        // Запрос к API наличия и парсинг данных
        var drugItems = await ParseDrugAvailability(dataItem, drug);

        return drugItems;
    }

    /// <summary>
    /// Извлекает data-item лекарства, необходимый для API-запросов.
    /// </summary>
    private string GetDrugDataItem(HtmlDocument doc)
    {
        var itemNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'compare_item')]");
        return itemNode?.GetAttributeValue("data-item", "не найдено") ?? "не найдено";
    }

    /// <summary>
    /// Получает данные о наличии лекарства в аптеках.
    /// </summary>
    private async Task<List<DrugItem>> ParseDrugAvailability(string dataItem, Drug? drug)
    {
        var url = $"https://vivafarm.md/ajax/productStoreAmountCustom.php?oid={dataItem}";
        var response = await FetchPageContent(url);
        if (response == null) return new List<DrugItem>();

        var doc = new HtmlDocument();
        doc.LoadHtml(response.Content);

        // Достаём данные о ценах, количестве и адресах
        var pricesNodes = doc.DocumentNode.SelectNodes("//span[contains(@class, 'price_seriya stock_text')]")
            ?.Where(n => Regex.IsMatch(n.InnerText.Trim(), @"\d+")).ToList() ?? new List<HtmlNode>();

        var countNodes = doc.DocumentNode
            .SelectNodes("//span[contains(@class, 'stock_seriya stock_text stock_margin')]")
            ?.Where(n => Regex.IsMatch(n.InnerText.Trim(), @"\d+")).ToList() ?? new List<HtmlNode>();

        var addressNodes = doc.DocumentNode
            .SelectNodes(".//a[contains(@class,'title_stores font_sm dark_link option-font-bold')]")
            ?.ToList() ?? new List<HtmlNode>();

        logger.LogInformation(
            "Найдено: Цены - {PricesCount}, Количество - {StockCount}, Адреса - {AddressesCount}",
            pricesNodes.Count, countNodes.Count, addressNodes.Count);

        var drugStores = await ParseDrugStores();
        var drugItems = new List<DrugItem>();

        for (int i = 0; i < addressNodes.Count; i++)
        {
            try
            {
                var count = double.Parse(countNodes[i].InnerText.Trim().Replace(",", "."), CultureInfo.InvariantCulture);
                var price = decimal.Parse(pricesNodes[i].InnerText.Replace("руб.", "").Trim(),
                    CultureInfo.InvariantCulture);
                var storeNumber = int.Parse(Regex.Replace(addressNodes[i].InnerText.Trim().Split(",")[0], @"\D", ""));
                var drugStore = drugStores.FirstOrDefault(x => x.Number == storeNumber);

                drugItems.Add(new DrugItem(count, price, drug, drugStore));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка парсинга товара по индексу {Index}", i);
            }
        }

        return drugItems;
    }

    private List<DrugStore>? _cachedDrugStores;

    /// <summary>
    /// Парсит список аптек с сайта с кешированием в памяти.
    /// </summary>
    public async Task<List<DrugStore>> ParseDrugStores(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (_cachedDrugStores != null && !forceRefresh)
        {
            return _cachedDrugStores;
        }

        logger.LogInformation("Парсинг списка аптек...");
        var url = "https://vivafarm.md/contacts/stores/";
        var response = await FetchPageContent(url, cancellationToken: cancellationToken);
        if (response == null) return _cachedDrugStores ?? new List<DrugStore>();

        var doc = new HtmlDocument();
        doc.LoadHtml(response.Content);

        var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'item bordered box-shadow')]");
        if (nodes == null || nodes.Count == 0)
        {
            logger.LogWarning("Аптеки не найдены.");
            return _cachedDrugStores ?? new List<DrugStore>();
        }

        var drugStores = new List<DrugStore>();
        foreach (var htmlNode in nodes.Skip(1))
        {
            try
            {
                var addressNode = htmlNode.SelectSingleNode(".//a[contains(@class,'darken')]");
                var telephoneNode = htmlNode.SelectSingleNode(".//a[contains(@class,'black')]");
                if (addressNode == null || telephoneNode == null) continue;

                var addressStr = addressNode.InnerText.Trim().Split(',');
                var storeNumber = int.Parse(addressStr[0].Split("№")[1]);
                var address = new Address(
                    addressStr[1].Split('.')[1].Trim(),
                    string.Join(".", addressStr[2].Split('.').Skip(1)).Trim(),
                    addressStr[3].Trim(),
                    33000
                );

                drugStores.Add(new DrugStore(address, storeNumber, telephoneNode.InnerText.Trim(), "vivafarm"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка обработки аптеки: {StoreText}", htmlNode.InnerText.Trim());
            }
        }

        _cachedDrugStores = drugStores;
        return drugStores;
    }

    /// <summary>
    /// Получает список ссылок на лекарства
    /// </summary>
    /// <returns></returns>
    public async Task<List<string>> ParseDrugsLinksAsync()
    {
        logger.LogInformation("Начинаем парсинг ссылок на лекарства...");

        var categoryLinks = await GetCategoryLinks();
        if (categoryLinks == null || categoryLinks.Count == 0)
        {
            logger.LogWarning("Категории не найдены.");
            return new List<string>();
        }

        logger.LogInformation("Найдено {CategoryCount} категорий. Парсим товары...", categoryLinks.Count);

        var productLinks = new HashSet<string>();
        foreach (var categoryLink in categoryLinks)
        {
            var productUrls = await GetProductLinksFromCategory(categoryLink) ?? new HashSet<string>();
            if (productUrls != null)
            {
                productLinks.UnionWith(productUrls);
            }
        }

        logger.LogInformation("Всего найдено {ProductCount} товаров.", productLinks.Count);
        return productLinks.ToList();
    }

    /// <summary>
    /// Получает список ссылок на категории лекарств.
    /// </summary>
    private async Task<List<string>> GetCategoryLinks()
    {
        var doc = await FetchHtmlDocument("https://vivafarm.md/catalog/lekarstva_i_bady/");
        if (doc == null) return new List<string>();

        var categoryNodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'section-compact-list__link')]");
        if (categoryNodes == null) return new List<string>();

        var categoryLinks = new List<string>();
        foreach (var node in categoryNodes)
        {
            var href = node.GetAttributeValue("href", "").Trim();
            if (!string.IsNullOrEmpty(href))
            {
                categoryLinks.Add($"https://vivafarm.md{href}");
            }
        }

        return categoryLinks;
    }

    /// <summary>
    /// Получает список ссылок на товары из категории.
    /// </summary>
    private async Task<HashSet<string>> GetProductLinksFromCategory(string categoryUrl)
    {
        var doc = await FetchHtmlDocument(categoryUrl);
        if (doc == null) 
        {
            logger.LogWarning("Не найдено товаров в категории {CategoryUrl}, ошибка запроса", categoryUrl);
            return new HashSet<string>();
        }

        int num = 1;
        try
        {
            num = int.Parse(doc.DocumentNode
                .SelectSingleNode("(//a[@class=\"dark_link\"])[last()]\n").InnerText.Trim());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось определить количество страниц для категории {CategoryUrl}, используется 1", categoryUrl);
            num = 1;
        }

        var productLinks = new HashSet<string>();
        for (var i = 1; i <= num; i++)
        {
            doc = await FetchHtmlDocument(categoryUrl + $"?PAGEN_1={i}");
            if (doc == null) return new HashSet<string>();

            var productNodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'thumb')]");
            if (productNodes == null) return new HashSet<string>();
            foreach (var node in productNodes)
            {
                var href = node.GetAttributeValue("href", "").Trim();
                if (!string.IsNullOrEmpty(href))
                {
                    productLinks.Add($"https://vivafarm.md{href}");
                }
            }

        }
        
        logger.LogInformation("Найдено {ProductCount} товаров в категории {CategoryUrl}", productLinks.Count, categoryUrl);
        return productLinks;
    }
}