using Microsoft.Extensions.Diagnostics.HealthChecks;
using Store.Shared.MessageBus;

namespace Store.GatewayService.HealthChecks;

public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly IMessageBusConnection _connection;
    private readonly ILogger<RabbitMQHealthCheck> _logger;

    public RabbitMQHealthCheck(IMessageBusConnection connection, ILogger<RabbitMQHealthCheck> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connection == null)
            {
                _logger.LogWarning("RabbitMQ connection service not available");
                return Task.FromResult(HealthCheckResult.Degraded("RabbitMQ connection service not configured"));
            }

            if (_connection.IsConnected)
            {
                _logger.LogDebug("RabbitMQ connection is healthy");
                return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ connection is healthy"));
            }
            else
            {
                _logger.LogWarning("RabbitMQ connection is not available");
                return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ connection is not available"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ health check failed", ex));
        }
    }
}
