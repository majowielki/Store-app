using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Store.Shared.MessageBus;

public class RabbitMQConnection : IMessageBusConnection
{
    private readonly ConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQConnection> _logger;
    private IConnection? _connection;
    private bool _disposed;
    private readonly object _syncRoot = new object();
    private int _retryCount = 0;
    private const int MaxRetryAttempts = 5;

    public RabbitMQConnection(ConnectionFactory connectionFactory, ILogger<RabbitMQConnection> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsConnected => _connection?.IsOpen == true && !_disposed;

    public bool TryConnect()
    {
        _logger.LogInformation("RabbitMQ Client is trying to connect to {HostName}:{Port}", 
            _connectionFactory.HostName, _connectionFactory.Port);

        lock (_syncRoot)
        {
            if (IsConnected)
            {
                _logger.LogDebug("RabbitMQ Client is already connected");
                return true;
            }

            var retryDelay = CalculateRetryDelay(_retryCount);
            
            try
            {
                // Log connection attempt details
                _logger.LogInformation("Attempting to connect to RabbitMQ: Host={HostName}, Port={Port}, VirtualHost={VirtualHost}, UserName={UserName}", 
                    _connectionFactory.HostName, 
                    _connectionFactory.Port, 
                    _connectionFactory.VirtualHost, 
                    _connectionFactory.UserName);

                _connection = _connectionFactory.CreateConnection();
                
                if (!IsConnected)
                {
                    _logger.LogCritical("FATAL ERROR: RabbitMQ connection was created but is not open. Connection state: {State}", 
                        _connection?.IsOpen);
                    return false;
                }

                // Subscribe to connection events
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;
                _connection.ConnectionUnblocked += OnConnectionUnblocked;

                _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", 
                    _connection.Endpoint.HostName);
                
                // Reset retry count on successful connection
                _retryCount = 0;
                return true;
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogCritical(ex, "FATAL ERROR: RabbitMQ broker is unreachable. Host={HostName}, Port={Port}. Retry attempt {RetryCount}/{MaxRetryAttempts}", 
                    _connectionFactory.HostName, _connectionFactory.Port, _retryCount + 1, MaxRetryAttempts);
                return HandleConnectionFailure(retryDelay);
            }
            catch (SocketException ex)
            {
                _logger.LogCritical(ex, "FATAL ERROR: Socket error connecting to RabbitMQ. Host={HostName}, Port={Port}. Error: {ErrorCode}. Retry attempt {RetryCount}/{MaxRetryAttempts}", 
                    _connectionFactory.HostName, _connectionFactory.Port, ex.SocketErrorCode, _retryCount + 1, MaxRetryAttempts);
                return HandleConnectionFailure(retryDelay);
            }
            catch (AuthenticationFailureException ex)
            {
                _logger.LogCritical(ex, "FATAL ERROR: Authentication failed connecting to RabbitMQ. Username={UserName}. Check credentials.", 
                    _connectionFactory.UserName);
                return false; // Don't retry authentication failures
            }
            catch (TimeoutException ex)
            {
                _logger.LogCritical(ex, "FATAL ERROR: Timeout connecting to RabbitMQ. Host={HostName}, Port={Port}, Timeout={Timeout}ms. Retry attempt {RetryCount}/{MaxRetryAttempts}", 
                    _connectionFactory.HostName, _connectionFactory.Port, _connectionFactory.RequestedConnectionTimeout.TotalMilliseconds, _retryCount + 1, MaxRetryAttempts);
                return HandleConnectionFailure(retryDelay);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "FATAL ERROR: Unexpected error connecting to RabbitMQ. Host={HostName}, Port={Port}. Retry attempt {RetryCount}/{MaxRetryAttempts}", 
                    _connectionFactory.HostName, _connectionFactory.Port, _retryCount + 1, MaxRetryAttempts);
                return HandleConnectionFailure(retryDelay);
            }
        }
    }

    private bool HandleConnectionFailure(TimeSpan retryDelay)
    {
        _retryCount++;
        
        if (_retryCount >= MaxRetryAttempts)
        {
            _logger.LogCritical("FATAL ERROR: Maximum retry attempts ({MaxRetryAttempts}) exceeded. Giving up on RabbitMQ connection.", MaxRetryAttempts);
            return false;
        }

        _logger.LogWarning("Will retry RabbitMQ connection in {RetryDelay}ms. Attempt {RetryCount}/{MaxRetryAttempts}", 
            retryDelay.TotalMilliseconds, _retryCount + 1, MaxRetryAttempts);
        
        Task.Delay(retryDelay).Wait();
        return TryConnect();
    }

    private static TimeSpan CalculateRetryDelay(int retryCount)
    {
        // Exponential backoff: 1s, 2s, 4s, 8s, 16s
        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
        var maxDelay = TimeSpan.FromSeconds(30);
        return delay > maxDelay ? maxDelay : delay;
    }

    public IDisposable CreateModel()
    {
        if (!IsConnected)
        {
            var connectResult = TryConnect();
            if (!connectResult)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }
        }

        try
        {
            return _connection!.CreateModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RabbitMQ model/channel");
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        lock (_syncRoot)
        {
            try
            {
                if (_connection != null)
                {
                    // Unsubscribe from events before disposing
                    _connection.ConnectionShutdown -= OnConnectionShutdown;
                    _connection.CallbackException -= OnCallbackException;
                    _connection.ConnectionBlocked -= OnConnectionBlocked;
                    _connection.ConnectionUnblocked -= OnConnectionUnblocked;

                    _connection.Dispose();
                    _logger.LogInformation("RabbitMQ connection disposed successfully");
                }
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex, "Error disposing RabbitMQ connection");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpected error disposing RabbitMQ connection");
            }
        }
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("RabbitMQ connection is blocked. Reason: {Reason}", e.Reason);
    }

    private void OnConnectionUnblocked(object? sender, EventArgs e)
    {
        if (_disposed) return;

        _logger.LogInformation("RabbitMQ connection is unblocked");
    }

    private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning(e.Exception, "RabbitMQ connection threw exception. Trying to re-connect...");

        Task.Run(() =>
        {
            try
            {
                TryConnect();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect after callback exception");
            }
        });
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;

        _logger.LogWarning("RabbitMQ connection is shutdown. Reason: {ReasonText} (Code: {ReplyCode}). Trying to re-connect...", 
            reason.ReplyText, reason.ReplyCode);

        Task.Run(() =>
        {
            try
            {
                TryConnect();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect after connection shutdown");
            }
        });
    }
}