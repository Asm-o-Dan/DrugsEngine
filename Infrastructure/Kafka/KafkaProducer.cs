using System.Collections.Concurrent;
using System.Text.Json;
using Application.Interfaces;
using Confluent.Kafka;
using Domain.Entities;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Kafka;

/// <summary>
/// Отправляет препараты в Kafka для векторизации (DrugsEnginePythonService). Один продюсер на приложение.
/// Возвращает только те Id, доставку которых подтвердил брокер (Acks.All) — остальные будут отправлены повторно.
/// </summary>
public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly ILogger<KafkaProducer> _logger;
    private readonly KafkaOptions _options;
    private readonly Lazy<IProducer<string, string>> _producer;

    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        _options = options.Value;
        _producer = new Lazy<IProducer<string, string>>(() =>
            new ProducerBuilder<string, string>(new ProducerConfig
                {
                    BootstrapServers = _options.BootstrapServers,
                    Acks = Acks.All,
                    EnableIdempotence = true,
                    MessageTimeoutMs = 15_000,
                })
                .SetErrorHandler((_, e) => _logger.LogWarning("Kafka: {Reason}", e.Reason))
                .Build());
    }

    public async Task<IReadOnlyCollection<Guid>> PublishAsync(IReadOnlyCollection<Drug> drugs, CancellationToken ct)
    {
        if (drugs.Count == 0) return [];
        var delivered = new ConcurrentBag<Guid>();
        var producer = _producer.Value;

        foreach (var drug in drugs)
        {
            // Формат согласован с DrugsEnginePythonService (Drug.from_json): PascalCase, Id обязателен.
            var payload = JsonSerializer.Serialize(new DrugMessage(
                drug.Id, drug.Name, drug.Manufacturer, drug.CountryCodeId, drug.Form, drug.ActiveIngredient));
            var id = drug.Id;
            producer.Produce(_options.Topic, new Message<string, string> { Key = id.ToString(), Value = payload }, report =>
            {
                if (!report.Error.IsError) delivered.Add(id);
            });
        }

        // Flush блокирующий — ждём подтверждений вне потока запросов.
        await Task.Run(() => producer.Flush(TimeSpan.FromSeconds(20)), ct);
        if (delivered.Count < drugs.Count)
            _logger.LogWarning("Kafka подтвердила {Delivered} из {Total} сообщений — остальные будут отправлены повторно",
                delivered.Count, drugs.Count);
        return delivered.Distinct().ToList();
    }

    public void Dispose()
    {
        if (!_producer.IsValueCreated) return;
        _producer.Value.Flush(TimeSpan.FromSeconds(10));
        _producer.Value.Dispose();
    }

    private sealed record DrugMessage(
        Guid Id, string Name, string Manufacturer, string? CountryCodeId, string? Form, string? ActiveIngredient);
}
