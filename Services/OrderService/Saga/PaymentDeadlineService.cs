using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.OrderService.Data;

namespace Store.OrderService.Saga;

/// <summary>
/// Looks for orders still waiting for their payment after the deadline and tells their saga,
/// which cancels them. The deadline is read from the database rather than scheduled as a
/// delayed message, so RabbitMQ needs no plugin and a restart loses nothing (ADR 013). An order
/// is told again on the next round until its saga has moved on, which it ignores.
/// </summary>
public sealed class PaymentDeadlineService : BackgroundService
{
    private const int Batch = 100;

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _time;
    private readonly OrderSagaOptions _options;
    private readonly ILogger<PaymentDeadlineService> _logger;

    public PaymentDeadlineService(IServiceScopeFactory scopes, TimeProvider time, IOptions<OrderSagaOptions> options, ILogger<PaymentDeadlineService> logger)
    {
        _scopes = scopes;
        _time = time;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.DeadlineCheckSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Payment deadline check failed; retrying on the next round");
            }
        }
    }

    /// <summary>One round: every order past its deadline gets <see cref="OrderPaymentTimedOut"/> through the outbox.</summary>
    public async Task<int> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopes.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var now = _time.GetUtcNow().UtcDateTime;
        const string waiting = nameof(OrderStateMachine.AwaitingPayment);
        var overdue = await context.OrderStates.AsNoTracking()
            .Where(s => s.CurrentState == waiting && s.PaymentDueAt <= now)
            .OrderBy(s => s.PaymentDueAt)
            .Take(Batch)
            .Select(s => new { s.OrderId, s.PaymentDueAt })
            .ToListAsync(cancellationToken);

        foreach (var order in overdue)
        {
            await publish.Publish(new OrderPaymentTimedOut(order.OrderId, order.PaymentDueAt!.Value), cancellationToken);
        }

        if (overdue.Count > 0)
        {
            // The bus outbox writes the messages with this save and sends them after it
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("{Count} orders passed their payment deadline", overdue.Count);
        }

        return overdue.Count;
    }
}
