using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Store.PaymentService.Data;
using Store.Tests.Integration.TestSupport;
using System.Collections.Concurrent;
using System.Net;

namespace Store.Tests.Integration.Payments;

/// <summary>
/// PaymentService against its real database, with the shop's webhook endpoint played by
/// <see cref="WebhookReceiver"/> and every log line kept in <see cref="Logs"/>.
/// </summary>
public sealed class PaymentApiFactory : StoreApiFactory<PaymentDbContext>
{
    public const string WebhookUrl = "http://shop.test/api/v1/webhooks/payments";

    public PaymentApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    public WebhookReceiver Webhooks { get; } = new();

    public LogCollector Logs { get; } = new();

    protected override string? DatabaseName => "store_payment_test";

    protected override void ConfigureSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("PaymentWebhooks:Url", WebhookUrl);
        builder.UseSetting("PaymentWebhooks:SigningSecret", TestTokens.WebhookSecret);
        builder.UseSetting("PaymentWebhooks:DispatchIntervalSeconds", "1");
        // Everything the service writes, to prove a card number is never among it
        builder.UseSetting("Logging:LogLevel:Default", "Trace");
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => Webhooks));
    }
}

/// <summary>Plays the shop's webhook endpoint: keeps every request and answers with <see cref="Status"/>.</summary>
public sealed class WebhookReceiver : HttpMessageHandler
{
    public ConcurrentQueue<(string Signature, string Body)> Received { get; } = new();

    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.ToString() != PaymentApiFactory.WebhookUrl)
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        var signature = request.Headers.TryGetValues("Store-Signature", out var values) ? values.Single() : string.Empty;
        Received.Enqueue((signature, await request.Content!.ReadAsStringAsync(cancellationToken)));
        return new HttpResponseMessage(Status);
    }
}

/// <summary>Keeps every log line of the host, formatted, with its exception.</summary>
public sealed class LogCollector : ILoggerProvider
{
    public ConcurrentQueue<string> Lines { get; } = new();

    public ILogger CreateLogger(string categoryName) => new Collector(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class Collector(LogCollector owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => owner.Lines.Enqueue($"{category}: {formatter(state, exception)} {exception}");
    }
}
