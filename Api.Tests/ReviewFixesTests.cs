using System.Net;
using System.Text;
using Application.Interfaces;
using Application.UseCases.Commands.DrugItemCommands;
using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Configuration;
using Infrastructure.Dal;
using Infrastructure.Dal.Maintenance;
using Infrastructure.Geocoding;
using Infrastructure.Kafka;
using Infrastructure.SemanticSearch;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Api.Tests;

/// <summary>Регрессионные тесты на дефекты, найденные ревью.</summary>
[Collection(nameof(ApiCollection))]
public sealed class ReviewFixesTests(ApiFactory factory)
{
    private async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    private static DrugItem Parsed(string drugName, string storeKey, decimal price, DateTime? reportedDate = null)
    {
        var store = new DrugStore(new Address("Тирасполь", "ул. Ревью", storeKey[^1..], 3300), 0, $"(533) {storeKey}", "Столетник");
        store.SetExternalKey(storeKey);
        var drug = new Drug(drugName, "Тест-Фарм", null, null);
        return new DrugItem(1, price, drug, store, reportedDate, StockLevel.Medium);
    }

    [Fact]
    public async Task PublishWorker_MarksOnlyDeliveredDrugs_AndRetriesTheRest()
    {
        var a = new Drug("Публикация А таб 10мг №10", "Тест-Фарм", null, null);
        var b = new Drug("Публикация Б таб 10мг №10", "Тест-Фарм", null, null);
        await InScope(async sp =>
        {
            var db = sp.GetRequiredService<DrugsBotDbContext>();
            // остальной каталог считаем уже опубликованным, чтобы проверять только эти два препарата
            await db.Drugs.ExecuteUpdateAsync(s => s.SetProperty(d => d.PublishedAt, DateTime.UtcNow));
            db.AddRange(a, b);
            return await db.SaveChangesAsync();
        });
        var worker = factory.Services.GetRequiredService<DrugPublishWorker>();

        factory.Kafka.Deliver = d => d.Id == a.Id; // брокер подтвердил только А
        Assert.Equal(1, await worker.PublishPendingAsync(100, CancellationToken.None));

        factory.Kafka.Deliver = _ => true;          // брокер снова доступен — Б уходит повторно
        Assert.Equal(1, await worker.PublishPendingAsync(100, CancellationToken.None));
        Assert.Equal(0, await worker.PublishPendingAsync(100, CancellationToken.None));
        Assert.Equal(2, factory.Kafka.Sent.Count(id => id == b.Id));
    }

    [Fact]
    public async Task ChangedDrugDetails_AreQueuedForReindexing()
    {
        const string name = "Переиндексация таб 20мг №30";
        await InScope(sp => sp.GetRequiredService<IMediator>().Send(new CreateOrUpdateDrugItemCommand(Parsed(name, "71000001", 10m))));
        await InScope(async sp =>
            await sp.GetRequiredService<DrugsBotDbContext>().Drugs.Where(d => d.Name == name)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.PublishedAt, DateTime.UtcNow)));

        // источник начал указывать действующее вещество
        var item = Parsed(name, "71000001", 10m);
        item.Drug!.SetDetails("Таблетки", "Тестовое вещество");
        await InScope(sp => sp.GetRequiredService<IMediator>().Send(new CreateOrUpdateDrugItemCommand(item)));

        var drug = await InScope(sp => sp.GetRequiredService<DrugsBotDbContext>().Drugs.SingleAsync(d => d.Name == name));
        Assert.Equal("Тестовое вещество", drug.ActiveIngredient);
        Assert.Null(drug.PublishedAt);
    }

    [Fact]
    public async Task PriceDate_WithoutSourceDate_ChangesOnlyWhenPriceChanges()
    {
        const string name = "Дата цены таб 5мг №20";
        await InScope(sp => sp.GetRequiredService<IMediator>().Send(new CreateOrUpdateDrugItemCommand(Parsed(name, "71000002", 30m))));
        var old = DateTime.UtcNow.AddDays(-2);
        await InScope(async sp =>
            await sp.GetRequiredService<DrugsBotDbContext>().DrugItems.Where(i => i.Drug!.Name == name)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.UpdatedAt, old)));

        // тот же прайс — дата цены не меняется, но предложение отмечено как увиденное
        await InScope(sp => sp.GetRequiredService<IMediator>().Send(new CreateOrUpdateDrugItemCommand(Parsed(name, "71000002", 30m))));
        var same = await InScope(sp => sp.GetRequiredService<DrugsBotDbContext>().DrugItems.SingleAsync(i => i.Drug!.Name == name));
        Assert.Equal(old, same.UpdatedAt, TimeSpan.FromSeconds(1));
        Assert.True(same.SeenAt > DateTime.UtcNow.AddMinutes(-1));

        // цена изменилась — дата обновилась
        await InScope(sp => sp.GetRequiredService<IMediator>().Send(new CreateOrUpdateDrugItemCommand(Parsed(name, "71000002", 31m))));
        var changed = await InScope(sp => sp.GetRequiredService<DrugsBotDbContext>().DrugItems.SingleAsync(i => i.Drug!.Name == name));
        Assert.True(changed.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task MergeService_RecomputesStaleKeys()
    {
        await InScope(async sp =>
        {
            var db = sp.GetRequiredService<DrugsBotDbContext>();
            var a = new Drug("Ключевой таб 50мг №10", "Ключ-Фарм", null, null);
            a.SetMatchKey("устаревший ключ от прошлой версии правила");
            var b = new Drug("Ключевой 50мг №10 таб.", "Ключ-Фарм", null, null);
            db.AddRange(a, b);
            return await db.SaveChangesAsync();
        });

        await InScope(sp => sp.GetRequiredService<DrugMergeService>().RunAsync(CancellationToken.None));

        var left = await InScope(sp => sp.GetRequiredService<DrugsBotDbContext>().Drugs.CountAsync(d => d.Name.StartsWith("Ключевой")));
        Assert.Equal(1, left);
    }
}

public sealed class SemanticClientAndGeocoderTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<string> Urls { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Urls.Add(request.RequestUri!.ToString());
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task BrokenJson_DoesNotBreakDrugPage_AndSearchReportsUnavailable()
    {
        var client = new SemanticSearchClient(
            new HttpClient(new StubHandler(_ => Json("{not json"))) { BaseAddress = new Uri("http://vectors/") },
            NullLogger<SemanticSearchClient>.Instance);

        Assert.Null(await client.GetDocumentAsync(Guid.NewGuid(), CancellationToken.None));
        await Assert.ThrowsAsync<SemanticSearchUnavailableException>(() => client.SearchAsync("изжога", 5, CancellationToken.None));
    }

    [Fact]
    public async Task Geocoder_DropsNoNumberMarker()
    {
        var handler = new StubHandler(_ => Json("""[{"lat":"46.5","lon":"29.9"}]"""));
        var geocoder = new NominatimGeocoder(
            new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.test/") },
            Options.Create(new GeocodingOptions { MinInterval = TimeSpan.Zero }),
            NullLogger<NominatimGeocoder>.Instance);

        var point = await geocoder.GeocodeAsync("Тея", "ул. Ленина", "б/н", CancellationToken.None);

        Assert.Equal(new GeoPoint(46.5, 29.9), point);
        var query = Uri.UnescapeDataString(handler.Urls.Single());
        Assert.Contains("q=ул. Ленина, Тея", query);
        Assert.DoesNotContain("б/н", query);
    }
}
