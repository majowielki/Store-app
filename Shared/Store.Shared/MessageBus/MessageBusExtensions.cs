using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Store.Shared.MessageBus;

public static class MessageBusExtensions
{
    public static IServiceCollection AddMessageBus(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddRabbitMQ(configuration);
    }

    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMQSettings = configuration.GetSection("RabbitMQ");
        
        services.AddSingleton<ConnectionFactory>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ConnectionFactory>>();
            
            var hostName = rabbitMQSettings["HostName"] ?? "localhost";
            var port = rabbitMQSettings.GetValue<int>("Port", 5672);
            var userName = rabbitMQSettings["UserName"] ?? "guest";
            var password = rabbitMQSettings["Password"] ?? "guest";
            var virtualHost = rabbitMQSettings["VirtualHost"] ?? "/";
            
            logger.LogInformation("Configuring RabbitMQ ConnectionFactory: Host={HostName}, Port={Port}, VirtualHost={VirtualHost}, UserName={UserName}",
                hostName, port, virtualHost, userName);

            var factory = new ConnectionFactory()
            {
                HostName = hostName,
                Port = port,
                UserName = userName,
                Password = password,
                VirtualHost = virtualHost,
                DispatchConsumersAsync = true,
                
                // Enhanced configuration for production
                RequestedHeartbeat = TimeSpan.FromSeconds(rabbitMQSettings.GetValue<int>("RequestedHeartbeat", 60)),
                AutomaticRecoveryEnabled = rabbitMQSettings.GetValue<bool>("AutomaticRecoveryEnabled", true),
                NetworkRecoveryInterval = TimeSpan.FromMilliseconds(rabbitMQSettings.GetValue<int>("NetworkRecoveryInterval", 5000)),
                RequestedConnectionTimeout = TimeSpan.FromMilliseconds(rabbitMQSettings.GetValue<int>("ConnectionTimeout", 30000)),
                
                // Additional reliability settings
                ContinuationTimeout = TimeSpan.FromMilliseconds(rabbitMQSettings.GetValue<int>("ContinuationTimeout", 20000)),
                HandshakeContinuationTimeout = TimeSpan.FromMilliseconds(rabbitMQSettings.GetValue<int>("HandshakeContinuationTimeout", 10000)),
                TopologyRecoveryEnabled = rabbitMQSettings.GetValue<bool>("TopologyRecoveryEnabled", true)
            };

            // Validate configuration
            ValidateRabbitMQConfiguration(factory, logger);

            return factory;
        });

        services.AddSingleton<IMessageBusConnection>(sp =>
        {
            var factory = sp.GetRequiredService<ConnectionFactory>();
            var logger = sp.GetRequiredService<ILogger<RabbitMQConnection>>();
            return new RabbitMQConnection(factory, logger);
        });

        services.AddSingleton<IMessageBus>(sp =>
        {
            var connection = sp.GetRequiredService<IMessageBusConnection>();
            var logger = sp.GetRequiredService<ILogger<RabbitMQMessageBus>>();
            var exchangeName = rabbitMQSettings["ExchangeName"] ?? "store_events";
            return new RabbitMQMessageBus(connection, logger, exchangeName);
        });

        return services;
    }

    private static void ValidateRabbitMQConfiguration(ConnectionFactory factory, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(factory.HostName))
        {
            logger.LogWarning("RabbitMQ HostName is null or empty, using default: localhost");
        }

        if (factory.Port <= 0 || factory.Port > 65535)
        {
            logger.LogWarning("RabbitMQ Port {Port} is invalid, should be between 1-65535", factory.Port);
        }

        if (string.IsNullOrWhiteSpace(factory.UserName))
        {
            logger.LogWarning("RabbitMQ UserName is null or empty, using default: guest");
        }

        if (factory.RequestedConnectionTimeout.TotalMilliseconds < 1000)
        {
            logger.LogWarning("RabbitMQ ConnectionTimeout {Timeout}ms is very low, consider increasing it", 
                factory.RequestedConnectionTimeout.TotalMilliseconds);
        }

        logger.LogInformation("RabbitMQ configuration validated successfully");
    }

    public static IServiceCollection AddMessageBusSubscriptions(this IServiceCollection services)
    {
        services.AddHostedService<MessageBusSubscriptionService>();
        return services;
    }
}