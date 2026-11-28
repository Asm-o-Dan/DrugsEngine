using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Confluent.Kafka;
using Domain.Entities;
using Infrastructure.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Kafka;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly ILogger<KafkaProducer> _logger;
    private readonly IProducer<Null, string> _producer;
    private readonly bool _disposeProducer;
    private readonly string _topic;
    private readonly string _bootstrapServers;
    private int _disposedState;

    public string Topic => _topic;
    public string BootstrapServers => _bootstrapServers;
    public bool IsDisposed => Volatile.Read(ref _disposedState) != 0;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = 16
    };

    public KafkaProducer(
        IConfiguration? configuration,
        ILogger<KafkaProducer> logger,
        IProducer<Null, string>? producer = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var bootstrapServers = configuration?["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            bootstrapServers = configuration?["Kafka:Broker"];
        }
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            bootstrapServers = configuration?["Kafka__BootstrapServers"];
        }
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            bootstrapServers = configuration?["Kafka__Broker"];
        }
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            bootstrapServers = configuration?["KAFKA_BROKER"];
        }
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            bootstrapServers = configuration?["KAFKA_BOOTSTRAP_SERVERS"];
        }
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            bootstrapServers = Environment.GetEnvironmentVariable("Kafka__BootstrapServers");
        }
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            bootstrapServers = Environment.GetEnvironmentVariable("Kafka__Broker");
        }
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BROKER");
        }
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");
        }
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            bootstrapServers = "kafka:9092";
        }
        _bootstrapServers = bootstrapServers.Trim();

        var topic = configuration?["Kafka:Topic"];
        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = configuration?["Kafka__Topic"];
        }
        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = configuration?["KAFKA_TOPIC"];
        }
        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = Environment.GetEnvironmentVariable("Kafka__Topic");
        }
        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC");
        }
        _topic = string.IsNullOrWhiteSpace(topic) ? "drugs" : topic.Trim();

        if (producer != null)
        {
            _producer = producer;
            _disposeProducer = false;
        }
        else
        {
            var config = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers,
                SecurityProtocol = SecurityProtocol.Plaintext,
                MessageTimeoutMs = 10000,
                Acks = Acks.All,
                EnableIdempotence = true,
                LingerMs = 5
            };

            _producer = new ProducerBuilder<Null, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    DrugsEngineTelemetry.KafkaProduceErrorsCounter.Add(1);
                    _logger.LogError("Kafka error: {Reason} (Fatal={IsFatal})", e.Reason, e.IsFatal);
                })
                .Build();
            _disposeProducer = true;
        }
    }

    public KafkaProducer(ILogger<KafkaProducer> logger, IProducer<Null, string>? producer = null)
        : this(null, logger, producer)
    {
    }

    public void ProduceDrug(Drug drug)
    {
        if (IsDisposed)
        {
            _logger.LogWarning("ProduceDrug called on disposed KafkaProducer");
            return;
        }

        if (drug == null)
        {
            _logger.LogWarning("ProduceDrug called with null drug entity");
            return;
        }

        using var activity = DrugsEngineTelemetry.KafkaActivitySource.StartActivity("Kafka.ProduceDrug", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination.name", _topic);
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

            _producer.Produce(_topic, message, deliveryReport =>
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
        if (Interlocked.Exchange(ref _disposedState, 1) != 0)
        {
            return;
        }

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

        GC.SuppressFinalize(this);
    }
}