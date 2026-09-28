using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Webhooks;
using Store.PaymentService.Data;
using Store.PaymentService.Models;

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

    /// <summary>At most this many webhooks per round; the rest wait for the next one.</summary>
    private const int Batch = 20;

    private readonly IServiceScopeFactory _scopes;
    private readonly IHttpClientFactory _http;
    private readonly TimeProvider _time;
    private readonly PaymentWebhookOptions _options;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(
        IServiceScopeFactory scopes,
        IHttpClientFactory http,
        TimeProvider time,
        IOptions<PaymentWebhookOptions> options,
        ILogger<WebhookDispatcher> logger)
    {
        _scopes = scopes;
        _http = http;
        _time = time;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.DispatchIntervalSeconds));
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

    /// <summary>One round: sends what is due, up to a batch. Returns how many were attempted.</summary>
    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken = default)
    {
        var attempted = 0;
        while (attempted < Batch && await DispatchNextAsync(cancellationToken))
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

        var now = _time.GetUtcNow().UtcDateTime;
        var due = await context.WebhookDeliveries
            .FromSqlInterpolated($"""
                SELECT * FROM "WebhookDeliveries"
                WHERE "DeliveredAt" IS NULL AND "FailedAt" IS NULL AND "NextAttemptAt" <= {now}
                ORDER BY "NextAttemptAt"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);
        if (due.Count == 0)
        {
            return false;
        }

        var delivery = due[0];
        var error = await SendAsync(delivery, cancellationToken);
        var after = _time.GetUtcNow().UtcDateTime;
        delivery.Attempts++;
        if (error is null)
        {
            delivery.DeliveredAt = after;
            delivery.LastError = null;
            _logger.LogInformation("Webhook {EventId} ({Type}) delivered on attempt {Attempt}", delivery.Id, delivery.Type, delivery.Attempts);
        }
        else if (WebhookSchedule.DelayAfter(delivery.Attempts) is { } delay)
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

    /// <summary>Posts the signed body; the error to record, or null when the shop answered 2xx.</summary>
    private async Task<string?> SendAsync(WebhookDelivery delivery, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Url)
        {
            Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add(WebhookSignature.HeaderName, WebhookSignature.Create(_options.SigningSecret, _time.GetUtcNow(), delivery.Payload));

        try
        {
            using var response = await _http.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return ex.Message.Length > 450 ? ex.Message[..450] : ex.Message;
        }
    }
}
