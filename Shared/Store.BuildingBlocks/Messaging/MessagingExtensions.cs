using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Configuration;

namespace Store.BuildingBlocks.Messaging;

/// <summary>
/// MassTransit over RabbitMQ with the transactional outbox and inbox in the service's own
/// database. Publishing inside a unit of work writes the message next to the data it
/// describes; a delivery service hands it to the broker afterwards, so a broker that is down
/// delays an event instead of losing it. Consumers are wrapped in the inbox, which drops a
/// redelivered message the consumer already processed.
/// </summary>
public static class MessagingExtensions
{
    /// <summary>Health check tag: reported on /health, does not gate readiness - the outbox buffers.</summary>
    public const string HealthTag = "messaging";

    /// <summary>
    /// Registers the bus for a service whose outbox and inbox tables live in
    /// <typeparamref name="TDbContext"/> (call <see cref="AddStoreMessagingTables"/> in its
    /// model). <paramref name="consumers"/> registers the consumers the service hosts.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="serviceName">
    /// Short name of the service ("cart", "identity"), prefixed to every queue name. Two services
    /// consuming the same event must not share a queue: with one queue the broker would hand
    /// each message to only one of them.
    /// </param>
    /// <param name="consumers">Registers the consumers the service hosts</param>
    public static IServiceCollection AddStoreMessaging<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<IBusRegistrationConfigurator>? consumers = null)
        where TDbContext : DbContext
    {
        services.AddStoreOptions<RabbitMqOptions>(configuration, RabbitMqOptions.SectionName);

        // Business actions go to the audit service as events, signed with the service name
        services.AddSingleton(new AuditTrailOptions(serviceName));
        services.AddScoped<IAuditTrail, BusAuditTrail<TDbContext>>();

        services.AddMassTransit(bus =>
        {
            // e.g. "cart-order-placed" for the cart's OrderPlacedConsumer
            bus.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(serviceName + "-", includeNamespace: false));

            bus.AddEntityFrameworkOutbox<TDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
                outbox.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
            });

            // Every receive endpoint gets the inbox, so consumers can be retried safely
            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                endpoint.UseEntityFrameworkOutbox<TDbContext>(context);
            });

            consumers?.Invoke(bus);

            // The broker is a dependency to report, not one to stop serving requests for
            bus.ConfigureHealthCheckOptions(options =>
            {
                options.Tags.Clear();
                options.Tags.Add(HealthTag);
                options.MinimalFailureStatus = HealthStatus.Degraded;
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                cfg.Host(options.Host, options.Port, options.VirtualHost, host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });

                // Three more attempts with growing delays; after that the message lands in the
                // endpoint's _error queue instead of looping back forever
                cfg.UseMessageRetry(retry => retry.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                cfg.PrefetchCount = 16;

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>Adds the outbox and inbox tables to a model. Requires a migration in the owning service.</summary>
    public static ModelBuilder AddStoreMessagingTables(this ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        return modelBuilder;
    }
}
