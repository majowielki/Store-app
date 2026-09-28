using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Store.BuildingBlocks.Serialization;
using Store.BuildingBlocks.Webhooks;
using Store.Contracts.Payments.V1;
using Store.Contracts.Payments.Webhooks;
using Store.OrderService.Data;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Orders;

/// <summary>
/// The order service's two ends of a payment: it opens the payment of an order waiting for one,
/// and it takes the payment service's signed webhooks - once each, only with a valid signature -
/// and turns them into the events its saga follows.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class OrderPaymentTests : IClassFixture<OrderApiFactory>
{
    private readonly OrderApiFactory _factory;
    private readonly OrderJourney _journey;

    public OrderPaymentTests(OrderApiFactory factory)
    {
        _factory = factory;
        _journey = new OrderJourney(factory);
    }

    private static PaymentWebhookEvent Event(string type, int orderId, decimal amount, Guid? paymentId = null, string? failureReason = null)
        => new(Guid.NewGuid(), type, DateTime.UtcNow,
            new PaymentWebhookData(paymentId ?? Guid.NewGuid(), orderId, amount, "usd", "visa", "4242", failureReason));

    /// <summary>Posts the event the way the payment service does, signed at <paramref name="signedAt"/> with <paramref name="secret"/>.</summary>
    private async Task<HttpResponseMessage> PostWebhookAsync(PaymentWebhookEvent webhook, string? secret = TestTokens.WebhookSecret, DateTimeOffset? signedAt = null)
    {
        var body = JsonSerializer.Serialize(webhook, StoreJson.CamelCase);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/payments")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        if (secret is not null)
        {
            request.Headers.Add(WebhookSignature.HeaderName, WebhookSignature.Create(secret, signedAt ?? DateTimeOffset.UtcNow, body));
        }

        using var client = _factory.CreateClient();
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> StartPaymentAsync(string user, int orderId)
    {
        using var client = _factory.CreateClient().AsUser(user);
        return await client.PostAsync($"/api/v1/orders/{orderId}/payment", content: null);
    }

    private int Published<T>(Func<T, bool> match) where T : class
        => _factory.Bus.Published.Select<T>(p => match(p.Context.Message)).Count();

    [Fact]
    public async Task Payment_is_opened_once_the_stock_is_held_and_every_call_gets_the_same_one()
    {
        const string user = "pay-start";
        var orderId = await _journey.PlaceAsync(user, 301, price: 450m);
        Assert.Equal(HttpStatusCode.Conflict, (await StartPaymentAsync(user, orderId)).StatusCode);

        await _journey.PublishAsync(OrderJourney.Reserved(orderId));
        await _journey.WaitForStatusAsync(orderId, "AwaitingPayment");
        var started = await StartPaymentAsync(user, orderId);
        var again = await StartPaymentAsync(user, orderId);

        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        var payment = await OrderJourney.ReadJson(started);
        var order = await _journey.OrderAsync(orderId);
        Assert.Equal(order.GetProperty("total").GetDecimal(), payment.GetProperty("amount").GetDecimal());
        Assert.Equal(order.GetProperty("paymentDueAt").GetDateTime(), payment.GetProperty("paymentDueAt").GetDateTime());
        Assert.Equal(payment.GetProperty("paymentId").GetGuid(), (await OrderJourney.ReadJson(again)).GetProperty("paymentId").GetGuid());
        Assert.Equal([$"order-{orderId}", $"order-{orderId}"], _factory.Upstreams.PaymentKeys.Where(k => k == $"order-{orderId}"));
        using var scope = _factory.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<OrderDbContext>().Orders.AsNoTracking().SingleAsync(o => o.Id == orderId);
        Assert.Equal(payment.GetProperty("paymentId").GetGuid(), stored.PaymentId);
    }

    [Fact]
    public async Task Nobody_else_pays_the_order_and_a_cancelled_one_cannot_be_paid()
    {
        var orderId = await _journey.AwaitingPaymentAsync("pay-mine", 302);
        Assert.Equal(HttpStatusCode.Forbidden, (await StartPaymentAsync("pay-someone-else", orderId)).StatusCode);

        using var admin = _factory.CreateClient().AsTrueAdmin();
        (await admin.PatchAsJsonAsync($"/api/v1/admin/orders/{orderId}/status", new { status = "Cancelled" })).EnsureSuccessStatusCode();

        var refused = await StartPaymentAsync("pay-mine", orderId);
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Contains("cancelled", (await OrderJourney.ReadJson(refused)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Signed_webhook_pays_the_order_once_however_often_it_arrives()
    {
        var orderId = await _journey.AwaitingPaymentAsync("pay-webhook", 303);
        var total = (await _journey.OrderAsync(orderId)).GetProperty("total").GetDecimal();
        var webhook = Event(PaymentWebhookTypes.Succeeded, orderId, total);

        var first = await PostWebhookAsync(webhook);
        var repeat = await PostWebhookAsync(webhook);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.False((await OrderJourney.ReadJson(first)).GetProperty("duplicate").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        Assert.True((await OrderJourney.ReadJson(repeat)).GetProperty("duplicate").GetBoolean());
        await _journey.WaitForStatusAsync(orderId, "Paid");
        Assert.Equal("4242", (await _journey.OrderAsync(orderId)).GetProperty("cardLast4").GetString());
        Assert.Equal(1, Published<PaymentAccepted>(m => m.OrderId == orderId));
    }

    [Fact]
    public async Task Webhook_with_a_wrong_missing_or_stale_signature_is_refused_and_changes_nothing()
    {
        var orderId = await _journey.AwaitingPaymentAsync("pay-forged", 304);
        var webhook = Event(PaymentWebhookTypes.Succeeded, orderId, 1m);

        var forged = await PostWebhookAsync(webhook, secret: "someone-elses-secret-that-is-long-enough-too");
        var unsigned = await PostWebhookAsync(webhook, secret: null);
        var replayed = await PostWebhookAsync(webhook, signedAt: DateTimeOffset.UtcNow.AddMinutes(-10));

        Assert.All([forged, unsigned, replayed], response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
        Assert.Equal("application/problem+json", forged.Content.Headers.ContentType?.MediaType);
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        Assert.Equal(0, Published<PaymentAccepted>(m => m.OrderId == orderId));
        Assert.Equal("AwaitingPayment", (await _journey.OrderAsync(orderId)).GetProperty("status").GetString());

        // The same event, signed properly, still goes through: its id was not spent by the forgeries
        Assert.Equal(HttpStatusCode.OK, (await PostWebhookAsync(webhook)).StatusCode);
    }

    [Fact]
    public async Task Refused_card_is_passed_on_with_the_customer_and_the_deadline_for_another_try()
    {
        var orderId = await _journey.AwaitingPaymentAsync("pay-declined-webhook", 305);
        var order = await _journey.OrderAsync(orderId);

        var response = await PostWebhookAsync(Event(PaymentWebhookTypes.Failed, orderId, 120m, failureReason: PaymentDeclineReasons.InsufficientFunds));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await Eventually.BecomesTrueAsync(() => Published<PaymentDeclined>(m => m.OrderId == orderId
            && m.Reason == PaymentDeclineReasons.InsufficientFunds
            && m.UserEmail == "pay-declined-webhook@test.local"
            && m.RetryUntil == order.GetProperty("paymentDueAt").GetDateTime()) == 1));
        Assert.Equal("AwaitingPayment", (await _journey.OrderAsync(orderId)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Paid_order_cancelled_by_the_administrator_is_refunded_through_the_webhooks()
    {
        var orderId = await _journey.AwaitingPaymentAsync("pay-refund-webhook", 306);
        var total = (await _journey.OrderAsync(orderId)).GetProperty("total").GetDecimal();
        var paymentId = Guid.NewGuid();
        await PostWebhookAsync(Event(PaymentWebhookTypes.Succeeded, orderId, total, paymentId));
        await _journey.WaitForStatusAsync(orderId, "Paid");

        using var admin = _factory.CreateClient().AsTrueAdmin();
        (await admin.PatchAsJsonAsync($"/api/v1/admin/orders/{orderId}/status", new { status = "Cancelled" })).EnsureSuccessStatusCode();
        Assert.True(await _journey.ConsumedAsync<PaymentRefundRequested>(e => e.OrderId == orderId && e.PaymentId == paymentId));

        await PostWebhookAsync(Event(PaymentWebhookTypes.Refunded, orderId, total, paymentId));

        await _journey.WaitForStatusAsync(orderId, "Refunded");
    }
}
