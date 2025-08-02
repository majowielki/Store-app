using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Store.Shared.MessageBus;

public static class MessageBusExtensions
{
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMQSettings = configuration.GetSection("RabbitMQ");
        
        services.AddSingleton<ConnectionFactory>(sp =>
        {
            var factory = new ConnectionFactory()
            {
                HostName = rabbitMQSettings["HostName"] ?? "localhost",
                Port = rabbitMQSettings.GetValue<int>("Port", 5672),
                UserName = rabbitMQSettings["UserName"] ?? "guest",
                Password = rabbitMQSettings["Password"] ?? "guest",
                VirtualHost = rabbitMQSettings["VirtualHost"] ?? "/",
                DispatchConsumersAsync = true
            };

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

    public static IServiceCollection AddMessageBusSubscriptions(this IServiceCollection services)
    {
        services.AddHostedService<MessageBusSubscriptionService>();
        return services;
    }
}