using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Persistence;
using Store.BuildingBlocks.Webhooks;
using Store.PaymentService.Data;
using Store.PaymentService.Models;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;

namespace Store.PaymentService.Webhooks;

/// <summary>
/// Sends the webhooks that are due, one at a time, each in its own transaction with its row
/// locked (SKIP LOCKED, so a second instance takes the next one). A 2xx settles it; anything else
/// puts it back on <see cref="WebhookSchedule"/> until the attempts run out. The body is signed at
/// the moment of sending, so the signature's timestamp is fresh on every retry.
/// </summary>
public sealed class WebhookDispatcher : BackgroundService
{
    public const string HttpClientName = "webhooks";

    private readonly IServiceScopeFactory _scopes;
    private readonly IHttpClientFactory _http;
    private readonly WebhookSchedule _schedule;
    private readonly TimeProvider _time;
    private readonly PaymentWebhookOptions _options;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(
        IServiceScopeFactory scopes,
        IHttpClientFactory http,
        WebhookSchedule schedule,
        TimeProvider time,
        IOptions<PaymentWebhookOptions> options,
        ILogger<WebhookDispatcher> logger)
    {
        _scopes = scopes;
        _http = http;
        _schedule = schedule;
        _time = time;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.DispatchInterval, _time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await DispatchDueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Webhook dispatch failed; retrying on the next round");
            }
        }
    }

    /// <summary>One round: sends what is due, up to <see cref="PaymentWebhookOptions.MaxPerRound"/>. Returns how many were attempted.</summary>
    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken = default)
    {
        var attempted = 0;
        while (attempted < _options.MaxPerRound && await DispatchNextAsync(cancellationToken))
        {
            attempted++;
        }

        return attempted;
    }

    private async Task<bool> DispatchNextAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Not composed on (a LIMIT around it would move the lock into a subquery): the SQL takes one row itself
        var delivery = (await context.WebhookDeliveries
            .FromSql(NextDue(context, _time.GetUtcNow().UtcDateTime))
            .ToListAsync(cancellationToken)).SingleOrDefault();
        if (delivery is null)
        {
            return false;
        }

        var error = await SendAsync(delivery, cancellationToken);
        var after = _time.GetUtcNow().UtcDateTime;
        delivery.Attempts++;
        if (error is null)
        {
            delivery.DeliveredAt = after;
            delivery.LastError = null;
            _logger.LogInformation("Webhook {EventId} ({Type}) delivered on attempt {Attempt}", delivery.Id, delivery.Type, delivery.Attempts);
        }
        else if (_schedule.DelayAfter(delivery.Attempts) is { } delay)
        {
            delivery.LastError = error;
            delivery.NextAttemptAt = after + delay;
            _logger.LogWarning("Webhook {EventId} ({Type}) failed on attempt {Attempt}: {Error}; next attempt in {Delay}",
                delivery.Id, delivery.Type, delivery.Attempts, error, delay);
        }
        else
        {
            delivery.LastError = error;
            delivery.FailedAt = after;
            _logger.LogError("Webhook {EventId} ({Type}) failed {Attempts} times and is given up: {Error}",
                delivery.Id, delivery.Type, delivery.Attempts, error);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>The oldest webhook due and not settled, locked; one another instance holds is skipped, not waited for.</summary>
    private static FormattableString NextDue(PaymentDbContext context, DateTime now)
    {
        var sql = context.Sql<WebhookDelivery>();
        var nextAttemptAt = sql.Column(d => d.NextAttemptAt);
        return FormattableStringFactory.Create(
            $"SELECT * FROM {sql.Table} " +
            $"WHERE {sql.Column(d => d.DeliveredAt)} IS NULL AND {sql.Column(d => d.FailedAt)} IS NULL AND {nextAttemptAt} <= {{0}} " +
            $"ORDER BY {nextAttemptAt} LIMIT 1 FOR UPDATE SKIP LOCKED",
            now);
    }

    /// <summary>Posts the signed body; the error to record, or null when the shop answered 2xx.</summary>
    private async Task<string?> SendAsync(WebhookDelivery delivery, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Url)
        {
            Content = new StringContent(delivery.Payload, Encoding.UTF8, MediaTypeNames.Application.Json)
        };
        request.Headers.Add(WebhookSignature.HeaderName, WebhookSignature.Create(_options.SigningSecret, _time.GetUtcNow(), delivery.Payload));

        try
        {
            using var response = await _http.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return ex.Message.Length > WebhookDelivery.LastErrorMaxLength ? ex.Message[..WebhookDelivery.LastErrorMaxLength] : ex.Message;
        }
    }
}
