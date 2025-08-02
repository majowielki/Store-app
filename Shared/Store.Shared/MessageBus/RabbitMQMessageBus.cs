using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using IModel = RabbitMQ.Client.IModel;

namespace Store.Shared.MessageBus;

public class RabbitMQMessageBus : IMessageBus, IDisposable
{
    private readonly IMessageBusConnection _connection;
    private readonly ILogger<RabbitMQMessageBus> _logger;
    private readonly string _exchangeName;
    private readonly Dictionary<string, IModel> _consumerChannels;
    private bool _disposed;

    public RabbitMQMessageBus(IMessageBusConnection connection, ILogger<RabbitMQMessageBus> logger, string exchangeName = "store_events")
    {
        _connection = connection;
        _logger = logger;
        _exchangeName = exchangeName;
        _consumerChannels = new Dictionary<string, IModel>();

        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        CreateExchange();
    }

    public async Task PublishAsync<T>(T message, string routingKey = "", CancellationToken cancellationToken = default) where T : IntegrationEvent
    {
        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        var eventName = typeof(T).Name;
        
        if (string.IsNullOrEmpty(routingKey))
        {
            routingKey = eventName.ToLowerInvariant();
        }

        _logger.LogTrace("Publishing event to RabbitMQ: {EventName} with routing key: {RoutingKey}", eventName, routingKey);

        using var channel = (IModel)_connection.CreateModel();
        
        var body = JsonSerializer.SerializeToUtf8Bytes(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // persistent

        channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger.LogTrace("Published event to RabbitMQ: {EventName}", eventName);
    }

    public async Task SubscribeAsync<T>(Func<T, Task> handler, string queueName = "", CancellationToken cancellationToken = default) where T : IntegrationEvent
    {
        var eventName = typeof(T).Name;
        
        if (string.IsNullOrEmpty(queueName))
        {
            queueName = $"{eventName.ToLowerInvariant()}_queue";
        }

        _logger.LogInformation("Subscribing to event: {EventName} with queue: {QueueName}", eventName, queueName);

        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        var channel = (IModel)_connection.CreateModel();
        _consumerChannels[queueName] = channel;

        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: queueName,
            exchange: _exchangeName,
            routingKey: eventName.ToLowerInvariant());

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var routingKey = ea.RoutingKey;
            var message = Encoding.UTF8.GetString(ea.Body.Span);

            try
            {
                _logger.LogTrace("Processing RabbitMQ event: {EventName}", routingKey);

                var integrationEvent = JsonSerializer.Deserialize<T>(message, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (integrationEvent != null)
                {
                    await handler(integrationEvent);
                }

                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error Processing message \"{Message}\"", message);
                
                // Reject and requeue the message
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    private void CreateExchange()
    {
        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        using var channel = (IModel)_connection.CreateModel();
        channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        foreach (var channel in _consumerChannels.Values)
        {
            try
            {
                channel?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing RabbitMQ channel");
            }
        }

        _consumerChannels.Clear();
        _connection?.Dispose();
    }
}