using Domain.Entities;
using Infrastructure.Parsing;
using Xunit;

namespace Api.Tests;

/// <summary>Разбор страниц 100letnik.md. Фикстуры повторяют структуру реальной разметки (ноябрь 2026).</summary>
public sealed class StoletnikSiteParserTests
{
    private static string StockItem(string region, string kind, string address, string phone, string stock, string price) => $"""
        <div class="item bordered box-shadow stock-item min-price" data-region="{region}">
          <div class="row">
            <div class="col-md-8 left-block-contacts">
              <div class="imgddd"><div class="title font_mxs darken">
                <div class="font-size-xxs text-muted text-uppercase">{region}</div>
                <div class="">{kind}</div>
              </div></div>
              <div class="top-wrap"><div class="title font_mxs darken"><div class="mb-4">
                <div class="font-size-xxs text-muted text-uppercase">Адрес:</div>
                <div class="">{address}</div>
              </div></div></div>
            </div>
            <div class="col-md-2 right-block-contacts"><div class="item-body"><div class="row"><div class="title font_mxs darken"><div class="mb-4">
              <div class="font-size-xxs text-muted text-uppercase">Телефон:</div>
              <div class="">{phone}</div>
            </div></div></div></div></div>
            <div class="col-md-2 right-block-contacts"><div class="item-body"><div class="row"><div class="title font_mxs darken"><div class="mb-4">
              <div class="font-size-xxs text-muted text-uppercase">На складе: <b style="font-weight: bold;">{stock}</b></div>
              <div class="font-size-xxs text-muted text-uppercase"><b style="color: #000000;font-size: 16px;">Цена: {price}</b></div>
              <div class="font-size-xxs text-muted text-uppercase">Партия: V43217 </div>
            </div></div></div></div></div>
          </div>
        </div>
        """;

    private static readonly string ProductHtml = $"""
        <html><body>
        <h1 class="global-header__title mb-0">Нольпаза 20мг №30 таб. (Пантопразол)</h1>
        <table class="s-features-wrapper w-100" id="s-product-features"><tr class="s-features-wrapper__feature">
          <td class="s-features-wrapper__name"><div class="s-features-wrapper__text pr-1">Производитель</div></td>
          <td class="s-features-wrapper__value pl-1">KRKA d.d. Novo mesto,  Словения</td></tr></table>
        <div id="sklad">
        {StockItem("Тирасполь", "-Интернет Аптека-", "", "", "Много", "44.9")}
        {StockItem("Тирасполь - &gt; Балка", "Аптека", "г. Тирасполь, ул. Одесская, 145", "0 (533) 66885", "Достаточно", "44.9")}
        {StockItem("Тирасполь - &gt; Центр", "Фармпункт", "г. Тирасполь, ул. Шевченко, 15 (Микрохирургия глаза)", "0 (533) 64455", "Мало", "46.3")}
        {StockItem("Рыбница", "Фармпункт", "г. Рыбница, ул. Вальченко 13-а/2", "0 (555) 42263", "Много", "45.15")}
        {StockItem("Тея", "Фармпункт", "с. Тея, ул. Ленина б/н", "0 (210) 61234", "Достаточно", "44.9")}
        </div></body></html>
        """;

    private const string ContactsHtml = """
        <div class="stores-list1">
        <div class="item bordered box-shadow"><div class="store-row">
          <div class="store-main">
            <div class="store-name"><div class="title font_mxs darken">
              <div class="store-label">Тирасполь - &gt; Балка</div><div class="store-value">Аптека</div></div></div>
            <div class="store-address"><div class="title font_mxs darken"><div class="mb-4">
              <div class="store-label">Адрес:</div><div class="store-value">г. Тирасполь, ул. Одесская, 145</div></div></div></div>
          </div>
          <div class="store-phone"><div class="item-body"><div class="title font_mxs darken"><div class="mb-4">
            <div class="store-label">Телефон:</div><div class="store-value">0 (533) 66885</div></div></div></div></div>
          <div class="store-hours"><div class="item-body"><div class="title font_mxs darken"><div class="mb-4">
            <div class="store-label">Время работы:</div><div class="store-value">Пн-Сб: 08:00 - 20:00<br>Вс: выходной</div></div></div></div></div>
        </div></div>
        </div>
        """;

    [Fact]
    public void ParseSitemap_KeepsOnlyProductPages()
    {
        const string xml = """
            <urlset>
              <url><loc>https://100letnik.md/category/medikamentyi/</loc></url>
              <url><loc>https://100letnik.md/kontakty/</loc></url>
              <url><loc>https://100letnik.md/nolpaza-20mg-30-tab---pantoprazol-/</loc></url>
              <url><loc>https://100letnik.md/nolpaza-20mg-30-tab---pantoprazol-/</loc></url>
              <url><loc>https://100letnik.md/zubnyie-kapli-20ml</loc></url>
            </urlset>
            """;
        Assert.Equal(
            new[] { "https://100letnik.md/nolpaza-20mg-30-tab---pantoprazol-/", "https://100letnik.md/zubnyie-kapli-20ml" },
            StoletnikSiteParser.ParseSitemap(xml));
    }

    [Fact]
    public void ParseProduct_ReadsNameManufacturerAndOffersPerPharmacy()
    {
        var skipped = new List<string>();
        var product = StoletnikSiteParser.ParseProduct(ProductHtml, skipped.Add)!;

        Assert.Equal("Нольпаза 20мг №30 таб. (Пантопразол)", product.Name);
        Assert.Equal(("KRKA d.d. Novo mesto", "SI"), (product.Manufacturer, product.CountryCode));
        Assert.Single(skipped); // интернет-аптека без адреса
        Assert.Equal(4, product.Offers.Count);

        var balka = product.Offers[0];
        Assert.Equal(new StoletnikStoreInfo("53366885", "Аптека", "Тирасполь - > Балка", "Тирасполь", "ул. Одесская", "145", "0 (533) 66885"), balka.Store);
        Assert.Equal((44.9m, StockLevel.Medium), (balka.Price, balka.Level));

        var center = product.Offers[1]; // цена в аптеке может отличаться; пометка в скобках не попадает в адрес
        Assert.Equal((46.3m, StockLevel.Low), (center.Price, center.Level));
        Assert.Equal(("ул. Шевченко", "15"), (center.Store.Street, center.Store.House));

        Assert.Equal(("ул. Вальченко", "13-А/2"), (product.Offers[2].Store.Street, product.Offers[2].Store.House));
        Assert.Equal(("Тея", "ул. Ленина", "б/н"), (product.Offers[3].Store.City, product.Offers[3].Store.Street, product.Offers[3].Store.House));
    }

    [Fact]
    public void ParseProduct_ReturnsNullForNonProductPage() =>
        Assert.Null(StoletnikSiteParser.ParseProduct("<html><body><p>О компании</p></body></html>"));

    [Fact]
    public void ParseContacts_NormalizesHours()
    {
        var contact = Assert.Single(StoletnikSiteParser.ParseContacts(ContactsHtml));
        Assert.Equal(new StoletnikContact("53366885", "Пн–Сб 8:00–20:00, Вс выходной"), contact);
    }

    [Theory]
    [InlineData("Пн-Вс: 08:00 - 20:00", "Пн–Вс 8:00–20:00")]
    [InlineData("Пн-Пт: 09:00 - 17:30<br>Сб: 09:00 - 13:30<br>Вс: выходной", "Пн–Пт 9:00–17:30, Сб 9:00–13:30, Вс выходной")]
    [InlineData("Пн-Сб: 08:00 - 20:00 |  Вс: 08:00 - 18:00", "Пн–Сб 8:00–20:00, Вс 8:00–18:00")]
    public void NormalizeHours(string source, string expected) =>
        Assert.Equal(expected, StoletnikSiteParser.NormalizeHours(source));

    [Theory]
    [InlineData("г. Бендеры, ул. Б. Восстания, 130а", "", "Бендеры", "ул. Б. Восстания", "130А")]
    [InlineData("с. Б.Хутор, ул. Советской Армии, 2б", "", "Б.Хутор", "ул. Советской Армии", "2Б")]
    [InlineData("с. Красная Горка, мкр. Консервный завод д.11 п.№3", "", "Красная Горка", "мкр. Консервный завод", "11 п.№3")]
    [InlineData("ул. Ленина, 1", "Григориополь", "Григориополь", "ул. Ленина", "1")]
    public void ParseAddress(string address, string district, string city, string street, string house) =>
        Assert.Equal((city, street, house), StoletnikSiteParser.ParseAddress(address, district));
}
