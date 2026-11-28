using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using Confluent.Kafka;
using Domain.Entities;
using Infrastructure.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Domain.Tests.Infrastructure;

public class KafkaProducerTest
{
    private readonly Mock<ILogger<KafkaProducer>> _loggerMock = new();
    private readonly Mock<IProducer<Null, string>> _mockProducer = new();

    [Fact]
    public void KafkaProducer_WithNullConfiguration_DefaultsToDrugsTopic()
    {
        // Arrange & Act
        var producer = new KafkaProducer(null, _loggerMock.Object, _mockProducer.Object);

        // Assert
        Assert.Equal("drugs", producer.Topic);
    }

    [Fact]
    public void KafkaProducer_WithEmptyConfiguration_DefaultsToDrugsTopic()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var producer = new KafkaProducer(configuration, _loggerMock.Object, _mockProducer.Object);

        // Assert
        Assert.Equal("drugs", producer.Topic);
    }

    [Fact]
    public void KafkaProducer_WithCustomConfiguration_UsesConfiguredTopic()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Kafka:Topic", "custom-pharma-topic"},
            {"Kafka:BootstrapServers", "localhost:9093"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        var producer = new KafkaProducer(configuration, _loggerMock.Object, _mockProducer.Object);

        // Assert
        Assert.Equal("custom-pharma-topic", producer.Topic);
    }

    [Fact]
    public void KafkaProducer_LegacyConstructor_DefaultsToDrugsTopic()
    {
        // Arrange & Act
        var producer = new KafkaProducer(_loggerMock.Object, _mockProducer.Object);

        // Assert
        Assert.Equal("drugs", producer.Topic);
    }

    [Fact]
    public void KafkaProducer_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KafkaProducer(null!, _mockProducer.Object));
    }

    [Fact]
    public void ProduceDrug_PublishesToConfiguredTopic_WithCorrectMessageAndTraceHeaders()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Kafka:Topic", "event-stream-topic"},
            {"Kafka:BootstrapServers", "localhost:9093"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        string capturedTopic = string.Empty;
        Message<Null, string>? capturedMessage = null;

        _mockProducer
            .Setup(p => p.Produce(
                It.IsAny<string>(),
                It.IsAny<Message<Null, string>>(),
                It.IsAny<Action<DeliveryReport<Null, string>>>()))
            .Callback<string, Message<Null, string>, Action<DeliveryReport<Null, string>>>(
                (topic, msg, _) =>
                {
                    capturedTopic = topic;
                    capturedMessage = msg;
                });

        var producer = new KafkaProducer(configuration, _loggerMock.Object, _mockProducer.Object);
        var country = new Country("Moldova", "MD");
        var drug = new Drug("Paracetamol 500mg", "PharmaCorp", "MD", country);

        // Act
        producer.ProduceDrug(drug);

        // Assert
        Assert.Equal("event-stream-topic", capturedTopic);
        Assert.NotNull(capturedMessage);
        Assert.Contains("Paracetamol 500mg", capturedMessage.Value);
        Assert.NotNull(capturedMessage.Headers);
    }

    [Fact]
    public void KafkaProducer_WithWhitespaceConfiguration_DefaultsToDrugsTopic()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Kafka:Topic", "   "},
            {"Kafka:BootstrapServers", "   "}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        var producer = new KafkaProducer(configuration, _loggerMock.Object, _mockProducer.Object);

        // Assert
        Assert.Equal("drugs", producer.Topic);
    }

    [Fact]
    public void ProduceDrug_WhenProducerThrowsException_HandlesGracefullyWithoutCrashing()
    {
        // Arrange
        _mockProducer
            .Setup(p => p.Produce(
                It.IsAny<string>(),
                It.IsAny<Message<Null, string>>(),
                It.IsAny<Action<DeliveryReport<Null, string>>>()))
            .Throws(new KafkaException(new Error(ErrorCode.BrokerNotAvailable, "Broker offline")));

        var producer = new KafkaProducer(null, _loggerMock.Object, _mockProducer.Object);
        var country = new Country("Moldova", "MD");
        var drug = new Drug("Ibuprofen", "Pharma", "MD", country);

        // Act & Assert
        var exception = Record.Exception(() => producer.ProduceDrug(drug));
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_ExternalProducer_DoesNotThrow()
    {
        // Arrange
        var producer = new KafkaProducer(null, _loggerMock.Object, _mockProducer.Object);

        // Act & Assert
        var exception = Record.Exception(() => producer.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void KafkaProducer_DefaultBootstrapServers_IsKafka9092()
    {
        // Arrange & Act
        var producer = new KafkaProducer(null, _loggerMock.Object, _mockProducer.Object);

        // Assert
        Assert.Equal("kafka:9092", producer.BootstrapServers);
    }

    [Fact]
    public void KafkaProducer_WithCustomBootstrapServers_ResolvesCorrectly()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Kafka:BootstrapServers", "192.168.1.100:9092"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        var producer = new KafkaProducer(configuration, _loggerMock.Object, _mockProducer.Object);

        // Assert
        Assert.Equal("192.168.1.100:9092", producer.BootstrapServers);
    }

    [Fact]
    public void KafkaProducer_FallbackToKafkaBrokerKey_WhenBootstrapServersMissing()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Kafka:Broker", "broker-host:9092"},
            {"KAFKA_TOPIC", "env-topic"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        var producer = new KafkaProducer(configuration, _loggerMock.Object, _mockProducer.Object);

        // Assert
        Assert.Equal("broker-host:9092", producer.BootstrapServers);
        Assert.Equal("env-topic", producer.Topic);
    }

    [Fact]
    public void ProduceDrug_WithNullDrug_DoesNotThrowException()
    {
        // Arrange
        var producer = new KafkaProducer(null, _loggerMock.Object, _mockProducer.Object);

        // Act & Assert
        var exception = Record.Exception(() => producer.ProduceDrug(null!));
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_IsIdempotent()
    {
        // Arrange
        var producer = new KafkaProducer(null, _loggerMock.Object, _mockProducer.Object);

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            producer.Dispose();
            producer.Dispose();
        });
        Assert.Null(exception);
    }

    [Fact]
    public void DependencyInjection_ResolvesKafkaProducer_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Kafka:Topic", "di-resolved-topic"},
            {"Kafka:BootstrapServers", "localhost:9093"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddSingleton<IProducer<Null, string>>(_mockProducer.Object);
        services.AddSingleton<IKafkaProducer, KafkaProducer>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var instance1 = serviceProvider.GetRequiredService<IKafkaProducer>();
        var instance2 = serviceProvider.GetRequiredService<IKafkaProducer>();

        // Assert
        Assert.NotNull(instance1);
        Assert.Same(instance1, instance2);
        var concreteProducer = Assert.IsType<KafkaProducer>(instance1);
        Assert.Equal("di-resolved-topic", concreteProducer.Topic);
        Assert.Equal("localhost:9093", concreteProducer.BootstrapServers);
    }

    [Fact]
    public void KafkaProducer_TrimsWhitespaceFromConfiguredTopicAndBootstrapServers()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Kafka:Topic", "   trimmed-topic   "},
            {"Kafka:BootstrapServers", "   localhost:9094   "}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        var producer = new KafkaProducer(configuration, _loggerMock.Object, _mockProducer.Object);

        // Assert
        Assert.Equal("trimmed-topic", producer.Topic);
        Assert.Equal("localhost:9094", producer.BootstrapServers);
    }

    [Fact]
    public void KafkaProducer_FallbackToKafkaBootstrapServersEnvKey()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"KAFKA_BOOTSTRAP_SERVERS", "env-bootstrap:9092"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        var producer = new KafkaProducer(configuration, _loggerMock.Object, _mockProducer.Object);

        // Assert
        Assert.Equal("env-bootstrap:9092", producer.BootstrapServers);
    }

    [Fact]
    public void KafkaProducer_FallbackToKafkaBrokerEnvKey()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"KAFKA_BROKER", "env-broker:9092"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        var producer = new KafkaProducer(configuration, _loggerMock.Object, _mockProducer.Object);

        // Assert
        Assert.Equal("env-broker:9092", producer.BootstrapServers);
    }

    [Fact]
    public void ProduceDrug_WhenDisposed_DoesNotProduceMessage()
    {
        // Arrange
        var producer = new KafkaProducer(null, _loggerMock.Object, _mockProducer.Object);
        var country = new Country("Moldova", "MD");
        var drug = new Drug("Aspirin", "Bayer", "MD", country);

        // Act
        producer.Dispose();
        producer.ProduceDrug(drug);

        // Assert
        _mockProducer.Verify(
            p => p.Produce(It.IsAny<string>(), It.IsAny<Message<Null, string>>(), It.IsAny<Action<DeliveryReport<Null, string>>>()),
            Times.Never);
    }

    [Fact]
    public void Dispose_ConcurrentInvocations_IsThreadSafe()
    {
        // Arrange
        var producer = new KafkaProducer(null, _loggerMock.Object, _mockProducer.Object);

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            System.Threading.Tasks.Parallel.For(0, 20, _ => producer.Dispose());
        });
        Assert.Null(exception);
        Assert.True(producer.IsDisposed);
    }

    [Fact]
    public void KafkaProducer_ResolvesDoubleUnderscoreAliases()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Kafka__BootstrapServers", "double-under-host:9092"},
            {"Kafka__Topic", "double-under-topic"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        var producer = new KafkaProducer(configuration, _loggerMock.Object, _mockProducer.Object);

        // Assert
        Assert.Equal("double-under-host:9092", producer.BootstrapServers);
        Assert.Equal("double-under-topic", producer.Topic);
    }

    [Fact]
    public void KafkaProducer_IsDisposed_InitiallyFalse_ThenTrueAfterDispose()
    {
        // Arrange
        var producer = new KafkaProducer(null, _loggerMock.Object, _mockProducer.Object);

        // Assert before
        Assert.False(producer.IsDisposed);

        // Act
        producer.Dispose();

        // Assert after
        Assert.True(producer.IsDisposed);
    }
}
