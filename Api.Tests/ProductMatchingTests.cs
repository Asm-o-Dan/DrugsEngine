using Application.UseCases.Commands.DrugItemCommands;
using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Dal;
using Infrastructure.Dal.Maintenance;
using Infrastructure.Parsing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests;

/// <summary>Сопоставление одного товара в разных сетях. Пары взяты из реальных прайсов «Доктора» и «Столетника».</summary>
public sealed class ProductKeyTests
{
    private static string? Key(string name, string manufacturer) =>
        ProductKey.Compute(name, manufacturer, ProductNameAnalyzer.DetectForm(name));

    [Theory]
    [InlineData("Нольпаза таб 20мг №30 (Пантопразол)", "KRKA", "Нольпаза 20мг №30 таб. (Пантопразол)", "KRKA d.d. Novo mesto")]
    [InlineData("Nestle NAN 1 (800 гр.)", "Nestle Suisse S.A.", "Nestle NAN 1 (800гр)", "NESTLE")]
    [InlineData("Флузамед капс 150мг №1(Флуконазол)", "World Medicine", "Флузамед 150мг №1 капс.(флуконазол)", "World Medicine")]
    [InlineData("Элицея таб п/п/о 20мг №30 (Эсциталопрам)", "KRKA", "Элицея 20мг №30 таб. (Эсциталопрам)", "KRKA d.d. Novo mesto")]
    [InlineData("Фармазолин капли назальные 0,05% 10мл д/детей (ксилометазолин)*", "Фармак",
        "Фармазолин 0,05% 10мл капли назальные для детей (Ксилометазолин)", "Фармак")]
    [InlineData("Холивер таб п/о №100", "DHG Pharma", "Холивер №100 таб.(эк-т желчи мед., эк-т артишока, куркума)", "DHG PHARMACEUTICAL JSC")]
    public void SameProductInDifferentNetworks_HasSameKey(string a, string ma, string b, string mb)
    {
        Assert.NotNull(Key(a, ma));
        Assert.Equal(Key(a, ma), Key(b, mb));
    }

    [Theory]
    [InlineData("Nestle NAN 1 (800гр)", "Nestle", "Nestle NAN 2 (800гр)", "Nestle")]
    [InlineData("Вода минеральная Куяльник 1,5л газ", "Куяльник", "Вода минеральная Куяльник 1,5л сильногаз", "Куяльник")]
    [InlineData("Боро Плюс крем 25мл (Фиолетовый)", "Emami Ltd.", "Боро Плюс крем 25мл (зеленый)", "Emami Ltd.")]
    [InlineData("Септолете Тотал пастилки 3мг/1мг №16(лимон и бузина)", "KRKA", "Септолете Тотал пастилки 3мг/1мг №16(лимон и мед)", "KRKA")]
    [InlineData("Нольпаза таб 20мг №30 (Пантопразол)", "KRKA", "Нольпаза таб 40мг №30 (Пантопразол)", "KRKA")]
    [InlineData("Нольпаза таб 20мг №30 (Пантопразол)", "KRKA", "Нольпаза таб 20мг №14 (Пантопразол)", "KRKA")]
    [InlineData("Нурофен Экспресс Форте капс 400мг №10", "Reckitt", "Нурофен Экспресс капс 400мг №10", "Reckitt")]
    [InlineData("Нольпаза таб 20мг №30", "KRKA", "Нольпаза таб 20мг №30", "Sandoz")]
    public void DifferentProducts_HaveDifferentKeys(string a, string ma, string b, string mb) =>
        Assert.NotEqual(Key(a, ma), Key(b, mb));

    [Theory]
    [InlineData("Нольпаза таб 20мг №30", "Не указан")]   // производитель неизвестен
    [InlineData("Пипетка в футляре", "Медполимер")]      // нет ни дозировки, ни фасовки
    public void WithoutReliableData_NoKey(string name, string manufacturer) => Assert.Null(Key(name, manufacturer));
}

[Collection(nameof(ApiCollection))]
public sealed class ProductMergeTests(ApiFactory factory)
{
    private static DrugItem Offer(string network, string storeKey, string drugName, string manufacturer, decimal price)
    {
        var store = new DrugStore(new Address("Тирасполь", "ул. Тестовая", storeKey[^2..], 3300), 0, $"(533) {storeKey}", network);
        store.SetExternalKey(storeKey);
        var drug = new Drug(drugName, manufacturer, "SI", new Country("Словения", "SI"));
        drug.SetDetails(ProductNameAnalyzer.DetectForm(drugName), ProductNameAnalyzer.DetectActiveIngredient(drugName));
        drug.SetMatchKey(ProductKey.Compute(drugName, manufacturer, drug.Form));
        return new DrugItem(1, price, drug, store);
    }

    [Fact]
    public async Task SameProductFromSecondNetwork_JoinsExistingDrug()
    {
        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new CreateOrUpdateDrugItemCommand(
                Offer("Доктор", "70000001", "Элицея таб п/п/о 10мг №30 (Эсциталопрам)", "KRKA", 84.95m)));
        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new CreateOrUpdateDrugItemCommand(
                Offer("Столетник", "70000002", "Элицея 10мг №30 таб. (Эсциталопрам)", "KRKA d.d. Novo mesto", 82.30m)));

        await using var check = factory.Services.CreateAsyncScope();
        var db = check.ServiceProvider.GetRequiredService<DrugsBotDbContext>();
        var drugs = await db.Drugs.Where(d => d.Name.StartsWith("Элицея")).ToListAsync();
        var drug = Assert.Single(drugs);
        var networks = await db.DrugItems.Where(i => i.DrugId == drug.Id).Select(i => i.DrugStore!.DrugNetwork).ToListAsync();
        Assert.Equal(new[] { "Доктор", "Столетник" }, networks.Order());
    }

    [Fact]
    public async Task MergeService_JoinsExistingDuplicates_KeepingFreshestPrice()
    {
        Guid keepId, dupId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DrugsBotDbContext>();
            // дубли без ключа — как в базе до появления сопоставления
            var store = new DrugStore(new Address("Тирасполь", "ул. Слияния", "1", 3300), 9001, "(533) 90001", "Доктор");
            store.SetExternalKey("53390001");
            var other = new DrugStore(new Address("Тирасполь", "ул. Слияния", "2", 3300), 9002, "(533) 90002", "Доктор");
            other.SetExternalKey("53390002");
            var a = new Drug("Хемомицин таб 500мг №3 (Азитромицин)", "Hemofarm", null, null);
            var b = new Drug("Хемомицин 500мг №3 таб.(Азитромицин)", "Hemofarm A D", null, null);
            db.AddRange(store, other, a, b);
            db.AddRange(
                new DrugItem(1, 47.00m, a, store, DateTime.UtcNow.AddHours(-5)),
                new DrugItem(1, 49.00m, a, other),
                new DrugItem(1, 47.75m, b, store));  // та же аптека, увидена позже — эта цена должна остаться
            await db.SaveChangesAsync();
            (keepId, dupId) = (a.Id, b.Id);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<DrugMergeService>().RunAsync(CancellationToken.None);

        await using var check = factory.Services.CreateAsyncScope();
        var dbc = check.ServiceProvider.GetRequiredService<DrugsBotDbContext>();
        Assert.False(await dbc.Drugs.AnyAsync(d => d.Id == dupId));
        var prices = await dbc.DrugItems.Where(i => i.DrugId == keepId).OrderBy(i => i.Cost).Select(i => i.Cost).ToListAsync();
        Assert.Equal(new[] { 47.75m, 49.00m }, prices);
    }
}
