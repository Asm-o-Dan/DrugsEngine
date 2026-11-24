using Application.UseCases.Commands.DrugItemCommands;
using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Dal;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests;

/// <summary>
/// Сохранение результатов парсера через настоящие обработчики MediatR и PostgreSQL.
/// Парсер на каждом проходе создаёт новые объекты с новыми Id — повторный проход не должен плодить дубли.
/// </summary>
[Collection(nameof(ApiCollection))]
public sealed class ParsingPipelineTests(ApiFactory factory)
{
    private const string StoreKey = "53377113";
    private const string DrugName = "Нольпаза таб 40 мг №30(Пантопразол)";

    private static DrugItem Parsed(decimal price, string hours, DateTime priceUpdatedAt)
    {
        var store = new DrugStore(new Address("Тирасполь", "ул. Ларионова", "43", 3300), 0, "(533) 7-71-13", "Доктор");
        store.SetExternalKey(StoreKey);
        store.UpdateDetails(store.Address, store.PhoneNumber, hours, 46.82, 29.61);

        var drug = new Drug(DrugName, "KRKA", "SI", new Country("Словения", "SI"));
        drug.SetDetails("Таблетки", "Пантопразол");
        return new DrugItem(1, price, drug, store, priceUpdatedAt);
    }

    private async Task SaveAsync(DrugItem item)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new CreateOrUpdateDrugItemCommand(item));
    }

    [Fact]
    public async Task SecondPass_UpdatesExistingRows_InsteadOfDuplicating()
    {
        var firstSeen = new DateTime(2026, 11, 20, 6, 0, 0, DateTimeKind.Utc);
        var secondSeen = new DateTime(2026, 11, 23, 6, 46, 0, DateTimeKind.Utc);

        await SaveAsync(Parsed(70m, "8:00–20:00", firstSeen));

        int numberAfterFirstPass;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            numberAfterFirstPass = (await scope.ServiceProvider.GetRequiredService<DrugsBotDbContext>()
                .DrugStores.SingleAsync(s => s.ExternalKey == StoreKey)).Number;
        }
        Assert.True(numberAfterFirstPass > 0);

        await SaveAsync(Parsed(65m, "8:00–21:00", secondSeen));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DrugsBotDbContext>();
            var store = await db.DrugStores.SingleAsync(s => s.ExternalKey == StoreKey);
            Assert.Equal(numberAfterFirstPass, store.Number); // номер не меняется между проходами
            Assert.Equal("8:00–21:00", store.Hours);
            Assert.Equal(46.82, store.Latitude);

            var drug = await db.Drugs.SingleAsync(d => d.Name == DrugName);
            Assert.Equal("Таблетки", drug.Form);
            Assert.Equal("Пантопразол", drug.ActiveIngredient);

            var item = await db.DrugItems.SingleAsync(i => i.DrugId == drug.Id);
            Assert.Equal(65m, item.Cost);
            Assert.Equal(secondSeen, item.UpdatedAt);
            Assert.True(item.SeenAt > DateTime.UtcNow.AddMinutes(-5));
        }
    }
}
