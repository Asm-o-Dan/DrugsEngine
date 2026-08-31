using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Confluent.Kafka;
using Domain.Entities;
using Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Kafka;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly ILogger<KafkaProducer> _logger;
    private readonly IProducer<Null, string> _producer;
    private readonly bool _disposeProducer;

    private const string Topic = "drugs";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = 16
    };

    private static readonly ProducerConfig DefaultConfig = new()
    {
        BootstrapServers = "kafka:9092",
        SecurityProtocol = SecurityProtocol.Plaintext,
        MessageTimeoutMs = 10000,
        Acks = Acks.All,
        EnableIdempotence = true,
        LingerMs = 5
    };

    public KafkaProducer(ILogger<KafkaProducer> logger, IProducer<Null, string>? producer = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (producer != null)
        {
            _producer = producer;
            _disposeProducer = false;
        }
        else
        {
            _producer = new ProducerBuilder<Null, string>(DefaultConfig)
                .SetErrorHandler((_, e) =>
                {
                    DrugsEngineTelemetry.KafkaProduceErrorsCounter.Add(1);
                    _logger.LogError("Kafka error: {Reason} (Fatal={IsFatal})", e.Reason, e.IsFatal);
                })
                .Build();
            _disposeProducer = true;
        }
    }

    public void ProduceDrug(Drug drug)
    {
        using var activity = DrugsEngineTelemetry.KafkaActivitySource.StartActivity("Kafka.ProduceDrug", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination.name", Topic);
        activity?.SetTag("drug.name", drug.Name);

        try
        {
            var headers = new Headers();
            if (activity != null && activity.Id != null)
            {
                headers.Add("traceparent", Encoding.UTF8.GetBytes(activity.Id));
                if (!string.IsNullOrEmpty(activity.TraceStateString))
                {
                    headers.Add("tracestate", Encoding.UTF8.GetBytes(activity.TraceStateString));
                }
            }

            var message = new Message<Null, string>
            {
                Value = JsonSerializer.Serialize(drug, JsonOptions),
                Headers = headers
            };

            _producer.Produce(Topic, message, deliveryReport =>
            {
                if (deliveryReport.Error.IsError)
                {
                    DrugsEngineTelemetry.KafkaProduceErrorsCounter.Add(1);
                    _logger.LogError("Kafka delivery failed for drug {DrugName}: {Reason}", drug.Name, deliveryReport.Error.Reason);
                }
                else
                {
                    DrugsEngineTelemetry.KafkaProducedCounter.Add(1);
                    _logger.LogDebug("Kafka message delivered for drug {DrugName} to partition {Partition} at offset {Offset}",
                        drug.Name, deliveryReport.Partition.Value, deliveryReport.Offset.Value);
                }
            });
        }
        catch (Exception ex)
        {
            DrugsEngineTelemetry.KafkaProduceErrorsCounter.Add(1);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to produce Kafka message for drug {DrugName}", drug.Name);
        }
    }

    public void Dispose()
    {
        if (_disposeProducer)
        {
            try
            {
                _producer.Flush(TimeSpan.FromSeconds(5));
                _producer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing Kafka producer");
            }
        }
    }
}