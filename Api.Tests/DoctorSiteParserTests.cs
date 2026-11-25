using Infrastructure.Parsing;
using Xunit;

namespace Api.Tests;

/// <summary>Разбор страниц aptekadoktor.com. Фикстуры повторяют структуру реальной разметки (ноябрь 2026).</summary>
public sealed class DoctorSiteParserTests
{
    private const string AvailabilityHtml = """
        <table>
          <thead><tr><td>Наименование</td><td>Производитель</td><td nowrap>Дата обвновления</td><td>Цена</td><td>Аптека</td></tr></thead>
          <tr>
            <td>Нольпаза таб 40 мг  №30(Пантопразол)</td>
            <td>KRKA, Словения</td>
            <td align="center" nowrap>23.11.26 08:46</td>
            <td nowrap>73.20 руб.</td>
            <td nowrap><a href="#" rel="show-map" data-src="https://maps.google.com/maps?f=q&amp;q=Strada&amp;sll=46.83,29.58&amp;ll=46.835978,29.624547&amp;z=14">г.Тирасполь, ул.Ленина 15 Б, тел.: 0 (533) 6-66-00</a></td>
          </tr>
          <tr>
            <td>Ибупрофен таб 200мг №50</td>
            <td>Турция</td>
            <td align="center" nowrap>01.07.26 10:00</td>
            <td nowrap>24.50 руб.</td>
            <td nowrap><a href="#" data-src="https://maps.google.com/maps?ll=46.625393,29.910952">г.Днестровск, ул.Строителей, тел.: 0 (219) 3-06-66</a></td>
          </tr>
          <tr>
            <td>Кремген мазь 30г</td>
            <td>Лаборатория Эксперт ; Россия</td>
            <td align="center" nowrap>23.11.26 08:46</td>
            <td nowrap>55.00 руб.</td>
            <td nowrap><a href="#" data-src="https://maps.google.com/">г.Тирасполь, ул.Калинина, 64, тел.: 0 (533) 5-66-36</a></td>
          </tr>
          <tr>
            <td>Без цены</td><td>Bayer</td><td>23.11.26 08:46</td><td>— руб.</td>
            <td><a href="#">г.Тирасполь, ул.Ленина 3 А, тел.: 0 (533) 7-44-62</a></td>
          </tr>
        </table>
        """;

    private const string ContactsHtml = """
        <p><span>Адреса аптек и фармпунктов в сети "Доктор":</span><br />
        <span>Аптека, ул. Ленина, 15 "б", тел. (533) 6-66-00, рабочие часы: 8:00-22:00 (напротив НК "Плазма")</span></p>
        <ul>
          <li><span>Фармпункт, ул. Калинина, 64, тел. (533) 5-66-36, рабочие часы: 8:00-20:00 (м-рн. "Кировский")</span></li>
          <li><span>Фармпункт, г. Днестровск ул Строителей ,тел(219)30-666 рабочие часы: 8:00-20:00</span></li>
          <li><span>Фармпункт, г. Днестровск Бульвар Энергетиков 12, тел (219)3-00-56, рабочие часы: 9:00-18:00</span></li>
        </ul>
        """;

    [Fact]
    public void ParseAvailability_ReadsRowsStoresAndCoordinates()
    {
        var skipped = new List<string>();
        var rows = DoctorSiteParser.ParseAvailability(AvailabilityHtml, skipped.Add);

        Assert.Equal(3, rows.Count);
        Assert.Single(skipped); // строка без цены

        var first = rows[0];
        Assert.Equal("Нольпаза таб 40 мг №30(Пантопразол)", first.Name); // фасовка «№30» не вырезается
        Assert.Equal("KRKA", first.Manufacturer);
        Assert.Equal("SI", first.CountryCode);
        Assert.Equal(73.20m, first.Price);
        // 08:46 по Кишинёву (зимой UTC+2) → 06:46 UTC
        Assert.Equal(new DateTime(2026, 11, 23, 6, 46, 0, DateTimeKind.Utc), first.UpdatedAtUtc);
        Assert.Equal("53366600", first.Store.Key);
        Assert.Equal("Тирасполь", first.Store.City);
        Assert.Equal("ул. Ленина", first.Store.Street);
        Assert.Equal("15Б", first.Store.House);
        Assert.Equal("(533) 6-66-00", first.Store.Phone);
        Assert.Equal(46.835978, first.Store.Latitude);
        Assert.Equal(29.624547, first.Store.Longitude);
    }

    [Fact]
    public void ParseAvailability_HandlesStoreWithoutHouseAndMapWithoutCoordinates()
    {
        var rows = DoctorSiteParser.ParseAvailability(AvailabilityHtml);

        var noHouse = rows[1].Store;
        Assert.Equal(("ул. Строителей", "б/н"), (noHouse.Street, noHouse.House));
        Assert.Equal(new DateTime(2026, 7, 1, 7, 0, 0, DateTimeKind.Utc), rows[1].UpdatedAtUtc); // летом UTC+3

        var noCoordinates = rows[2].Store;
        Assert.Equal(("ул. Калинина", "64"), (noCoordinates.Street, noCoordinates.House));
        Assert.Null(noCoordinates.Latitude);
    }

    [Theory]
    [InlineData("KRKA, Словения", "KRKA", "SI")]
    [InlineData("Лаборатория Эксперт ; Россия", "Лаборатория Эксперт", "RU")]
    [InlineData("Турция", DoctorSiteParser.UnknownManufacturer, "TR")]
    [InlineData("КИТАЙ", DoctorSiteParser.UnknownManufacturer, "CN")]
    [InlineData("Balkan Pharmaceuticals SRL Молдова", "Balkan Pharmaceuticals SRL", "MD")]
    [InlineData("Patheon France/уп-но ФФ Сотекс, Франция/Россия", "Patheon France/уп-но ФФ Сотекс", "FR")]
    [InlineData("Gedeon Richter", "Gedeon Richter", null)]
    [InlineData("Фарм-Ю, ПМР", "Фарм-Ю", null)] // нет кода ISO — страну не выдумываем
    public void SplitManufacturer_HandlesAllSourceFormats(string cell, string manufacturer, string? countryCode)
    {
        var result = DoctorSiteParser.SplitManufacturer(cell);
        Assert.Equal(manufacturer, result.Manufacturer);
        Assert.Equal(countryCode, result.CountryCode);
    }

    [Fact]
    public void ParseContacts_ExtractsPhoneKeyAddressAndHours()
    {
        var contacts = DoctorSiteParser.ParseContacts(ContactsHtml);

        Assert.Equal(4, contacts.Count);
        Assert.Equal(new DoctorContactInfo("53366600", "ленина|15", "8:00–22:00"), contacts[0]);
        // «тел» внутри «Строителей» не обрезает адрес
        Assert.Equal(new DoctorContactInfo("21930666", "строителей|", "8:00–20:00"), contacts[2]);
    }

    [Fact]
    public void AddressKey_MatchesSameStoreAcrossPages()
    {
        // в прайсе телефон Днестровска другой, сопоставляем по адресу
        Assert.Equal(
            DoctorSiteParser.AddressKey("Бульвар Энергетиков 12"),
            DoctorSiteParser.ParseContacts(ContactsHtml)[3].AddressKey);
        Assert.Equal(DoctorSiteParser.AddressKey("ул. К. Либкнехта 159Б"), DoctorSiteParser.AddressKey(" ул. К. Либкнехта, 159 \"б\""));
    }

    [Theory]
    [InlineData("Нольпаза таб 40 мг №30(Пантопразол)", "Таблетки", "Пантопразол")]
    [InlineData("Глаупрост капли глазные р-р 0,005% 2,5мл(Латанопрост)", "Капли", "Латанопрост")]
    [InlineData("Роноцит р-р д/ин 1000мг/4мл №5(Цитиколин)", "Раствор", "Цитиколин")]
    [InlineData("Ринобэби капли назальные 0,01% 10мл (оксиметазолин)", "Капли", "Оксиметазолин")]
    [InlineData("Иновум 40мг/5мг таб №28 (Олмесартан+Амлодипин)", "Таблетки", "Олмесартан + Амлодипин")]
    [InlineData("Комбиприл таб 5мг/10мг №30 (Амлодипин\\лизиноприл)", "Таблетки", "Амлодипин + лизиноприл")]
    [InlineData("Пивные дрожжи таб №100 (д/волос, кожи, ногтей)", "Таблетки", null)]
    [InlineData("Эндокринол капс №30 (БАД)", "Капсулы", null)]
    [InlineData("Бандаж для голеностопного сустава р-р1 (обхват голеностопа 18-23см)", null, null)]
    [InlineData("Ножницы маникюрные ELLY с цветной ручкой (06562)", null, null)]
    [InlineData("Шампунь MARRY MOORE Keratin 600мл (с кератином)", null, null)]
    [InlineData("Боро Плюс крем 25мл (Фиолетовый)", "Крем", null)]
    [InlineData("Септолете Тотал пастилки 3мг/1мг №16(лимон и мед)", "Пастилки", null)]
    public void ProductNameAnalyzer_DetectsFormAndIngredientConservatively(string name, string? form, string? ingredient)
    {
        Assert.Equal(form, ProductNameAnalyzer.DetectForm(name));
        Assert.Equal(ingredient, ProductNameAnalyzer.DetectActiveIngredient(name));
    }
}
