using System;
using System.Text;
using System.Threading;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using IModel = RabbitMQ.Client.IModel;

class Program
{
    private static IConnection _connection;
    private static IModel _channel;
    private static string _replyQueueName;
    private static EventingBasicConsumer _consumer;
    private static readonly ManualResetEvent _mre = new ManualResetEvent(false);
    private static string _correlationId;
    private static string _response;

    static void Main(string[] args)
    {
        Console.WriteLine("RabbitMQ Client Application");
        Console.WriteLine("1. Send message");
        Console.WriteLine("2. Start consumer");
        Console.Write("Select mode: ");
        
        var mode = Console.ReadLine();
        
        InitializeRabbitMQ();

        switch (mode)
        {
            case "1":
                SendMessageMode();
                break;
            case "2":
                StartConsumerMode();
                break;
            default:
                Console.WriteLine("Invalid mode selected");
                break;
        }

        Cleanup();
    }

    private static void InitializeRabbitMQ()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest",
            Port = 5672
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Для получения ответов
        _replyQueueName = _channel.QueueDeclare().QueueName;
        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += (model, ea) =>
        {
            if (ea.BasicProperties.CorrelationId == _correlationId)
            {
                _response = Encoding.UTF8.GetString(ea.Body.ToArray());
                _mre.Set();
            }
        };
    }

    private static void SendMessageMode()
    {
        Console.Write("Enter message text: ");
        var text = Console.ReadLine();

        _correlationId = Guid.NewGuid().ToString();
        var props = _channel.CreateBasicProperties();
        props.CorrelationId = _correlationId;
        props.ReplyTo = _replyQueueName;

        var message = new
        {
            text = text,
            timestamp = DateTime.UtcNow
        };

        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

        _channel.BasicPublish(
            exchange: "",
            routingKey: "rpc_queue", // Замените на вашу очередь
            basicProperties: props,
            body: body);

        Console.WriteLine($"Sent: {text}");

        _channel.BasicConsume(
            consumer: _consumer,
            queue: _replyQueueName,
            autoAck: true);

        // Ждем ответа 10 секунд
        if (_mre.WaitOne(10000))
        {
            Console.WriteLine($"Response: {_response}");
        }
        else
        {
            Console.WriteLine("No response received");
        }
    }

    private static void StartConsumerMode()
    {
        Console.WriteLine("Starting consumer...");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var props = ea.BasicProperties;

            Console.WriteLine($"Received: {message}");

            // Если нужно отправить ответ
            if (!string.IsNullOrEmpty(props.ReplyTo))
            {
                var response = new
                {
                    status = "processed",
                    originalMessage = message,
                    processedAt = DateTime.UtcNow
                };

                var responseBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: props.ReplyTo,
                    basicProperties: _channel.CreateBasicProperties(),
                    body: responseBody);
            }
        };

        _channel.BasicConsume(
            queue: "drug_processing_queue", // Замените на вашу очередь
            autoAck: true,
            consumer: consumer);

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static void Cleanup()
    {
        _channel?.Close();
        _connection?.Close();
    }
}