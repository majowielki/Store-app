using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Store.BuildingBlocks.Webhooks;
using Store.Contracts.Audit.V1;
using Store.Contracts.Orders.V1;
using Store.Contracts.Payments.V1;
using Store.PaymentService.Data;
using Store.PaymentService.Webhooks;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Payments;

/// <summary>
/// The payment service end to end on its side: the shop opens a payment, the customer pays with a
/// test card (3-D Secure included), the outcome leaves as a signed webhook that is retried until
/// the shop takes it, a cancelled order cancels its payment and a refund request returns the money.
/// The full card number is kept nowhere - not in the database, not in the logs.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class PaymentTests : IClassFixture<PaymentApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static int _lastOrderId = 7000;

    private readonly PaymentApiFactory _factory;

    public PaymentTests(PaymentApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    private HttpClient Shop()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Api-Key", TestTokens.InternalApiKey);
        return client;
    }

    /// <summary>A payment opened by the shop for a new order of <paramref name="user"/>.</summary>
    private async Task<(Guid Id, int OrderId)> OpenAsync(string user, decimal amount = 250m)
    {
        var orderId = Interlocked.Increment(ref _lastOrderId);
        using var shop = Shop();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/internal")
        {
            Content = JsonContent.Create(new { orderId, userId = user, amount, currency = "usd" })
        };
        request.Headers.Add("Idempotency-Key", $"order-{orderId}");
        var response = await shop.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return ((await ReadJson(response)).GetProperty("id").GetGuid(), orderId);
    }

    private static object Card(string number) => new { cardNumber = number, expMonth = 12, expYear = DateTime.UtcNow.Year + 2, cvc = "123" };

    private Task<HttpResponseMessage> ConfirmAsync(string user, Guid paymentId, string number)
        => _factory.CreateClient().AsUser(user).PostAsJsonAsync($"/api/v1/payments/{paymentId}/confirm", Card(number));

    /// <summary>The webhooks the shop received about a payment, their bodies parsed.</summary>
    private List<(string Signature, JsonElement Event)> WebhooksFor(Guid paymentId)
        => _factory.Webhooks.Received
            .Select(w => (w.Signature, Event: JsonSerializer.Deserialize<JsonElement>(w.Body, Json)))
            .Where(w => w.Event.GetProperty("data").GetProperty("paymentId").GetGuid() == paymentId)
            .ToList();

    private async Task<JsonElement> WebhookAsync(Guid paymentId, string type)
    {
        JsonElement found = default;
        Assert.True(await Eventually.BecomesTrueAsync(() =>
        {
            var match = WebhooksFor(paymentId).Where(w => w.Event.GetProperty("type").GetString() == type).ToList();
            if (match.Count > 0) found = match[0].Event;
            return match.Count > 0;
        }), $"no {type} webhook for payment {paymentId}");
        return found;
    }

    [Fact]
    public async Task The_service_migrates_its_database_and_reports_ready()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Only_the_shop_opens_payments_and_opening_twice_gives_the_same_one()
    {
        using var anonymous = _factory.CreateClient();
        using var customer = _factory.CreateClient().AsUser("pay-open");
        var body = new { orderId = 9001, userId = "pay-open", amount = 10m, currency = "usd" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/payments/internal", body)).StatusCode);
        // A customer's token is no internal key
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.PostAsJsonAsync("/api/v1/payments/internal", body)).StatusCode);

        var (id, orderId) = await OpenAsync("pay-open", 99.50m);

        using var shop = Shop();
        async Task<HttpResponseMessage> Again(decimal amount)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/internal")
            {
                Content = JsonContent.Create(new { orderId, userId = "pay-open", amount, currency = "usd" })
            };
            request.Headers.Add("Idempotency-Key", $"order-{orderId}");
            return await shop.SendAsync(request);
        }

        var again = await Again(99.50m);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(id, (await ReadJson(again)).GetProperty("id").GetGuid());
        Assert.Equal("requiresPaymentMethod", (await ReadJson(again)).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await Again(120m)).StatusCode);
    }

    [Fact]
    public async Task Card_4242_pays_and_the_shop_hears_it_by_a_signed_webhook()
    {
        var (id, orderId) = await OpenAsync("pay-4242", 871.20m);

        var confirmed = await ConfirmAsync("pay-4242", id, "4242 4242 4242 4242");

        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        var payment = await ReadJson(confirmed);
        Assert.Equal("succeeded", payment.GetProperty("status").GetString());
        Assert.Equal("visa", payment.GetProperty("cardBrand").GetString());
        Assert.Equal("4242", payment.GetProperty("cardLast4").GetString());

        var webhook = await WebhookAsync(id, "payment.succeeded");
        Assert.Equal(orderId, webhook.GetProperty("data").GetProperty("orderId").GetInt32());
        Assert.Equal(871.20m, webhook.GetProperty("data").GetProperty("amount").GetDecimal());
        var (signature, _) = WebhooksFor(id).First();
        var body = _factory.Webhooks.Received.First(w => w.Signature == signature).Body;
        Assert.Equal(WebhookSignatureCheck.Valid, WebhookSignature.Verify(signature, body, TestTokens.WebhookSecret, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5)));

        // Paid once; a second click finds it paid
        Assert.Equal(HttpStatusCode.Conflict, (await ConfirmAsync("pay-4242", id, "4242 4242 4242 4242")).StatusCode);
        Assert.True(await Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed
            .Select<AuditEvent>(e => e.Context.Message.Action == "PAYMENT_SUCCEEDED" && e.Context.Message.EntityId == id.ToString()).Any()));
    }

    [Theory]
    [InlineData("4000 0000 0000 0002", "card-declined")]
    [InlineData("4000 0000 0000 9995", "insufficient-funds")]
    public async Task Refused_card_leaves_the_payment_open_for_another_one(string number, string reason)
    {
        var (id, _) = await OpenAsync($"pay-refused-{reason}");

        var refused = await ReadJson(await ConfirmAsync($"pay-refused-{reason}", id, number));

        Assert.Equal("requiresPaymentMethod", refused.GetProperty("status").GetString());
        Assert.Equal(reason, refused.GetProperty("declineReason").GetString());
        var failed = await WebhookAsync(id, "payment.failed");
        Assert.Equal(reason, failed.GetProperty("data").GetProperty("failureReason").GetString());

        var paid = await ReadJson(await ConfirmAsync($"pay-refused-{reason}", id, "4242424242424242"));
        Assert.Equal("succeeded", paid.GetProperty("status").GetString());
        Assert.False(paid.TryGetProperty("declineReason", out _));
    }

    [Fact]
    public async Task Card_3220_asks_for_3d_secure_and_pays_once_approved()
    {
        var (id, _) = await OpenAsync("pay-3ds");
        using var customer = _factory.CreateClient().AsUser("pay-3ds");

        var challenged = await ReadJson(await ConfirmAsync("pay-3ds", id, "4000 0000 0000 3220"));
        Assert.Equal("requiresAction", challenged.GetProperty("status").GetString());
        Assert.Equal("3220", challenged.GetProperty("cardLast4").GetString());
        // While the check is open, no other card is taken
        Assert.Equal(HttpStatusCode.Conflict, (await ConfirmAsync("pay-3ds", id, "4242424242424242")).StatusCode);

        var approved = await ReadJson(await customer.PostAsJsonAsync($"/api/v1/payments/{id}/authenticate", new { approve = true }));

        Assert.Equal("succeeded", approved.GetProperty("status").GetString());
        await WebhookAsync(id, "payment.succeeded");
    }

    [Fact]
    public async Task Rejected_3d_secure_check_refuses_the_card()
    {
        var (id, _) = await OpenAsync("pay-3ds-no");
        using var customer = _factory.CreateClient().AsUser("pay-3ds-no");
        await ConfirmAsync("pay-3ds-no", id, "4000000000003220");

        var rejected = await ReadJson(await customer.PostAsJsonAsync($"/api/v1/payments/{id}/authenticate", new { approve = false }));

        Assert.Equal("requiresPaymentMethod", rejected.GetProperty("status").GetString());
        Assert.Equal("authentication-failed", rejected.GetProperty("declineReason").GetString());
        await WebhookAsync(id, "payment.failed");
        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsJsonAsync($"/api/v1/payments/{id}/authenticate", new { approve = true })).StatusCode);
    }

    [Theory]
    [InlineData("5555 5555 5555 4444")] // real-looking, but not a test card
    [InlineData("4242 4242 4242 4241")] // a digit wrong
    public async Task Only_test_cards_are_charged_and_the_number_is_not_repeated(string number)
    {
        var (id, _) = await OpenAsync("pay-not-a-test-card");

        var refused = await ConfirmAsync("pay-not-a-test-card", id, number);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        var text = await refused.Content.ReadAsStringAsync();
        Assert.DoesNotContain(number.Replace(" ", ""), text.Replace(" ", ""));
        Assert.Empty(WebhooksFor(id));
    }

    [Fact]
    public async Task Another_customer_cannot_see_or_pay_a_payment()
    {
        var (id, _) = await OpenAsync("pay-owner");
        using var stranger = _factory.CreateClient().AsUser("pay-stranger");

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/v1/payments/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ConfirmAsync("pay-stranger", id, "4242424242424242")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/payments/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Cancelled_order_cannot_be_paid_any_more()
    {
        var (id, orderId) = await OpenAsync("pay-cancelled");

        await _factory.Bus.Bus.Publish(new OrderCancelled(orderId, "pay-cancelled", OrderCancellationReasons.PaymentTimedOut, DateTime.UtcNow));

        using var customer = _factory.CreateClient().AsUser("pay-cancelled");
        await Eventually.AssertAsync(async () =>
            Assert.Equal("cancelled", (await ReadJson(await customer.GetAsync($"/api/v1/payments/{id}"))).GetProperty("status").GetString()));
        Assert.Equal(HttpStatusCode.Conflict, (await ConfirmAsync("pay-cancelled", id, "4242424242424242")).StatusCode);
    }

    [Fact]
    public async Task Refund_returns_the_money_once_and_tells_the_shop()
    {
        var (id, orderId) = await OpenAsync("pay-refund", 300m);
        await ConfirmAsync("pay-refund", id, "4242424242424242");

        var request = new PaymentRefundRequested(orderId, id, 300m, RefundReasons.OrderCancelled, DateTime.UtcNow);
        await _factory.Bus.Bus.Publish(request);
        await _factory.Bus.Bus.Publish(request);

        var refunded = await WebhookAsync(id, "payment.refunded");
        Assert.Equal(300m, refunded.GetProperty("data").GetProperty("amount").GetDecimal());
        using var customer = _factory.CreateClient().AsUser("pay-refund");
        Assert.Equal("refunded", (await ReadJson(await customer.GetAsync($"/api/v1/payments/{id}"))).GetProperty("status").GetString());
        await Task.Delay(TimeSpan.FromSeconds(2));
        Assert.Single(WebhooksFor(id), w => w.Event.GetProperty("type").GetString() == "payment.refunded");
    }

    [Fact]
    public async Task Webhook_the_shop_refuses_is_retried_on_the_schedule_and_given_up_after_five_attempts()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var dispatcher = _factory.Services.GetRequiredService<WebhookDispatcher>();
        _factory.Webhooks.Status = HttpStatusCode.InternalServerError;
        try
        {
            var (id, _) = await OpenAsync("pay-retries");
            await ConfirmAsync("pay-retries", id, "4242424242424242");

            var delivery = await Eventually.BecomesTrueAsync(async () =>
                await context.WebhookDeliveries.AsNoTracking().AnyAsync(d => d.PaymentId == id && d.Attempts == 1));
            Assert.True(delivery);
            var first = await context.WebhookDeliveries.AsNoTracking().SingleAsync(d => d.PaymentId == id);
            Assert.Equal("HTTP 500", first.LastError);
            Assert.InRange(first.NextAttemptAt - DateTime.UtcNow, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1.1));

            // The next four attempts, each as soon as it is due
            for (var attempt = 2; attempt <= 5; attempt++)
            {
                await context.WebhookDeliveries.Where(d => d.Id == first.Id)
                    .ExecuteUpdateAsync(d => d.SetProperty(x => x.NextAttemptAt, DateTime.UtcNow.AddSeconds(-1)));
                await dispatcher.DispatchDueAsync();
            }

            var last = await context.WebhookDeliveries.AsNoTracking().SingleAsync(d => d.Id == first.Id);
            Assert.Equal(5, last.Attempts);
            Assert.NotNull(last.FailedAt);
            Assert.Null(last.DeliveredAt);
        }
        finally
        {
            _factory.Webhooks.Status = HttpStatusCode.OK;
        }
    }

    [Fact]
    public async Task Webhook_that_failed_once_is_delivered_by_its_retry_with_the_same_event_id()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var dispatcher = _factory.Services.GetRequiredService<WebhookDispatcher>();
        _factory.Webhooks.Status = HttpStatusCode.ServiceUnavailable;
        Guid id;
        try
        {
            (id, _) = await OpenAsync("pay-retry-ok");
            await ConfirmAsync("pay-retry-ok", id, "4242424242424242");
            Assert.True(await Eventually.BecomesTrueAsync(async () =>
                await context.WebhookDeliveries.AsNoTracking().AnyAsync(d => d.PaymentId == id && d.Attempts == 1)));
        }
        finally
        {
            _factory.Webhooks.Status = HttpStatusCode.OK;
        }

        await context.WebhookDeliveries.Where(d => d.PaymentId == id)
            .ExecuteUpdateAsync(d => d.SetProperty(x => x.NextAttemptAt, DateTime.UtcNow.AddSeconds(-1)));
        await dispatcher.DispatchDueAsync();

        var delivered = await context.WebhookDeliveries.AsNoTracking().SingleAsync(d => d.PaymentId == id);
        Assert.NotNull(delivered.DeliveredAt);
        Assert.Equal(2, delivered.Attempts);
        var events = WebhooksFor(id).Select(w => w.Event.GetProperty("id").GetGuid()).ToList();
        Assert.Equal(2, events.Count);
        Assert.Single(events.Distinct());
    }

    [Fact]
    public async Task The_full_card_number_is_kept_nowhere()
    {
        var (id, _) = await OpenAsync("pay-no-pan");
        await ConfirmAsync("pay-no-pan", id, "4000 0000 0000 0002");
        await ConfirmAsync("pay-no-pan", id, "4242 4242 4242 4242");
        await WebhookAsync(id, "payment.succeeded");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var stored = new List<string>();
        string[] tables =
        [
            """SELECT t::text AS "Value" FROM "Payments" t""",
            """SELECT t::text AS "Value" FROM "PaymentAttempts" t""",
            """SELECT t::text AS "Value" FROM "WebhookDeliveries" t""",
            """SELECT t::text AS "Value" FROM "OutboxMessage" t""",
            """SELECT t::text AS "Value" FROM "InboxState" t""",
        ];
        foreach (var sql in tables)
        {
            stored.AddRange(await context.Database.SqlQueryRaw<string>(sql).ToListAsync());
        }

        var everything = string.Join('\n', stored.Concat(_factory.Logs.Lines).Concat(_factory.Webhooks.Received.Select(w => w.Body)));
        Assert.Contains("4242", everything);
        foreach (var number in new[] { "4242424242424242", "4242 4242 4242 4242", "4000000000000002", "4000 0000 0000 0002" })
        {
            Assert.DoesNotContain(number, everything);
        }
    }
}
