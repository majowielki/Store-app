using MassTransit.Serialization;
using Store.Contracts.Audit.V1;
using Store.Contracts.Catalog.V1;
using Store.Contracts.Orders.V1;
using Store.Contracts.Payments.V1;
using Store.Contracts.Reviews.V1;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Unit.Shared;

/// <summary>
/// The events services exchange travel as JSON written by MassTransit's serializer. Every
/// versioned contract must come back from the wire unchanged, with the camelCase names the
/// consumers in other services read; a contract without a sample here fails the coverage test.
/// </summary>
public class ContractSerializationTests
{
    private static readonly JsonSerializerOptions Wire = SystemTextJsonMessageSerializer.Options;

    private static readonly DateTime At = new(2026, 9, 28, 12, 30, 15, DateTimeKind.Utc);
    private static readonly Guid PaymentId = Guid.Parse("6f1d2c3b-4a59-4e6f-8a7b-9c0d1e2f3a4b");

    private static readonly IReadOnlyList<OrderItem> Items =
    [
        new OrderItem(12, "Oak Dining Table", 1, 899.00m),
        new OrderItem(40, "Linen Cushion", 2, 34.50m)
    ];

    /// <summary>One filled-in instance of every message a service publishes.</summary>
    public static readonly TheoryData<object> Messages = new()
    {
        new OrderPlaced(7, "user-1", "ann@example.com", "Ann Smith", "1 Test Street", true, 968.00m, 96.80m, 0m, 871.20m,
            [new OrderPlacedLine(12, "Oak Dining Table", 1, 899.00m)], At),
        new OrderStatusChanged(7, "user-1", "Paid", "Shipped", "admin-1", At),
        new OrderStatusChanged(7, "user-1", "AwaitingPayment", "Paid", null, At),
        new OrderPaid(7, "user-1", "ann@example.com", "Ann Smith", 871.20m, "visa", "4242", Items,
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 3), At),
        new OrderCancelled(7, "user-1", OrderCancellationReasons.PaymentTimedOut, At),
        new OrderShipped(7, "user-1", "ann@example.com", "Ann Smith", Items, new DateOnly(2026, 10, 1), null, At),
        new OrderRefunded(7, "user-1", 871.20m, At),
        new PaymentAccepted(PaymentId, 7, 871.20m, "visa", "4242", At),
        new PaymentDeclined(PaymentId, 7, "user-1", "ann@example.com", "Ann Smith", PaymentDeclineReasons.InsufficientFunds, At.AddMinutes(15), At),
        new PaymentRefunded(PaymentId, 7, 871.20m, At),
        new PaymentRefundRequested(7, PaymentId, 871.20m, RefundReasons.OrderCancelled, At),
        new PaymentRefundRequested(7, null, 871.20m, RefundReasons.PaidAfterCancellation, At),
        new StockReserved(7, [new StockLine(12, 1), new StockLine(40, 2)], At),
        new StockUnavailable(7, [new StockShortage(12, "Oak Dining Table", 2, 1)], At),
        new StockReleased(7, [], At),
        new ProductBackInStock(12, "Oak Dining Table", "oak-dining-table", "https://example.test/oak.webp", 899.00m, "ann@example.com", At),
        new ReviewSummaryChanged(12, 4.33m, 3, At),
        new AuditEvent("PRODUCT_UPDATED", "Product", "12", "admin-1", "catalog", At, """{"stock":4}""", null, """{"stock":5}""")
    };

    [Theory]
    [MemberData(nameof(Messages))]
    public void Message_survives_the_round_trip(object message)
    {
        var type = message.GetType();
        var json = JsonSerializer.Serialize(message, type, Wire);

        var back = JsonSerializer.Deserialize(json, type, Wire);

        Assert.NotNull(back);
        Assert.Equal(json, JsonSerializer.Serialize(back, type, Wire));
    }

    [Theory]
    [MemberData(nameof(Messages))]
    public void Every_field_is_written_under_its_camel_case_name(object message)
    {
        var json = JsonSerializer.SerializeToElement(message, message.GetType(), Wire);

        var written = json.EnumerateObject().Select(p => p.Name).ToList();
        var expected = message.GetType().GetProperties().Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name)).ToList();
        Assert.Equal(expected.Order(), written.Order());
    }

    [Fact]
    public void Order_cancelled_has_the_wire_shape_consumers_read()
    {
        // Compact, whatever indentation the bus writes with
        var json = JsonSerializer.Serialize(JsonSerializer.SerializeToElement(new OrderCancelled(7, "user-1", OrderCancellationReasons.OutOfStock, At), Wire));

        Assert.Equal("""{"orderId":7,"userId":"user-1","reason":"out-of-stock","cancelledAt":"2026-09-28T12:30:15Z"}""", json);
    }

    [Fact]
    public void Money_keeps_its_cents()
    {
        var json = JsonSerializer.Serialize(new OrderRefunded(7, "user-1", 0.10m + 0.20m, At), Wire);

        var back = JsonSerializer.Deserialize<OrderRefunded>(json, Wire)!;

        Assert.Equal(0.30m, back.Amount);
    }

    [Fact]
    public void Every_versioned_contract_has_a_sample()
    {
        var sampled = ((IEnumerable<object[]>)Messages).Select(row => row[0].GetType()).ToHashSet();
        // Lines and shortages travel inside the messages that list them
        var carried = sampled
            .SelectMany(t => t.GetProperties())
            .Select(p => p.PropertyType)
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            .Select(t => t.GetGenericArguments()[0])
            .ToHashSet();

        var contracts = typeof(OrderPlaced).Assembly.GetExportedTypes()
            .Where(t => t.Namespace?.EndsWith(".V1", StringComparison.Ordinal) == true)
            .Where(t => !(t.IsAbstract && t.IsSealed)) // static classes of constants
            .ToList();

        var missing = contracts.Where(t => !sampled.Contains(t) && !carried.Contains(t)).Select(t => t.FullName).ToList();
        Assert.Empty(missing);
    }
}
