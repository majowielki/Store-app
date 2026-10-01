using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Shop;
using Store.Contracts.Catalog.V1;
using Store.Contracts.Orders.V1;
using Store.Contracts.Payments.V1;
using Store.NotificationService.Consumers;
using Store.NotificationService.Mail;
using Store.NotificationService.Mail.Templates;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Store.Tests.Unit.NotificationService;

/// <summary>
/// The e-mails as the customers get them, pinned by snapshots next to this file (Snapshots/*.html,
/// *.txt): a changed template fails here until its snapshot is renewed on purpose with the
/// environment variable UPDATE_SNAPSHOTS=1.
/// </summary>
public class MailTemplatesTests
{
    private const string Shop = "https://shop.example/";
    private static readonly ShopLinks Links = new(Options.Create(new ShopOptions { Url = Shop }));
    private static readonly OrderPaidMail PaidMail = new(Links);
    private static readonly PaymentDeclinedMail DeclinedMail = new(Links);
    private static readonly OrderShippedMail ShippedMail = new(Links);
    private static readonly BackInStockMail BackInStockMail = new(Links);

    private static readonly IReadOnlyList<OrderItem> Lines =
    [
        new(7, "Oak Writing Desk", 1, 549.99m),
        new(12, "Linen Cushion Cover Set", 2, 39.50m)
    ];

    public static OrderPaid Paid(string customerName = "Anna Nowak") => new(
        104, "user-1", "anna@example.test", customerName, 628.99m, "visa", "4242", Lines,
        new DateOnly(2026, 9, 30), new DateOnly(2026, 10, 2), new DateTime(2026, 9, 28, 10, 15, 0, DateTimeKind.Utc));

    private static PaymentDeclined Declined(string reason) => new(
        Guid.Parse("6f1d2c3b-4a59-4e6f-8a7b-9c0d1e2f3a4b"), 104, "user-1", "anna@example.test", "Anna Nowak", reason,
        new DateTime(2026, 9, 28, 10, 30, 0, DateTimeKind.Utc), new DateTime(2026, 9, 28, 10, 16, 0, DateTimeKind.Utc));

    private static readonly OrderShipped Shipped = new(
        104, "user-1", "anna@example.test", "Anna Nowak", Lines, new DateOnly(2026, 9, 30), new DateOnly(2026, 9, 30),
        new DateTime(2026, 9, 29, 8, 0, 0, DateTimeKind.Utc));

    private static readonly ProductBackInStock BackInStock = new(
        31, "Paper Arc Floor Lamp", "paper-arc-floor-lamp", "https://images.example/lamp.webp", 189m, "visitor@example.test",
        new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc));

    public static TheoryData<string> Kinds => new() { OrderPaidMail.Kind, PaymentDeclinedMail.Kind, OrderShippedMail.Kind, BackInStockMail.Kind };

    private static Email Render(string kind) => kind switch
    {
        OrderPaidMail.Kind => PaidMail.Render(Paid()),
        PaymentDeclinedMail.Kind => DeclinedMail.Render(Declined(PaymentDeclineReasons.InsufficientFunds)),
        OrderShippedMail.Kind => ShippedMail.Render(Shipped),
        BackInStockMail.Kind => BackInStockMail.Render(BackInStock),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Every_email_matches_its_snapshot(string kind)
    {
        var email = Render(kind);

        Assert.Equal(kind, email.Kind);
        Snapshot.Match($"{kind}.html", email.Html);
        Snapshot.Match($"{kind}.txt", email.Text);
    }

    [Fact]
    public void Each_email_goes_to_the_customer_with_a_subject_and_links_into_the_shop()
    {
        Assert.Equal(("anna@example.test", "Thank you for your order #104"), (Render("order-paid").To, Render("order-paid").Subject));
        Assert.Contains("href=\"https://shop.example/orders/104\"", Render("order-paid").Html);
        Assert.Contains("href=\"https://shop.example/orders/104/pay\"", Render("payment-declined").Html);
        Assert.Equal("Your order #104 is on its way", Render("order-shipped").Subject);
        Assert.Equal(("visitor@example.test", "Paper Arc Floor Lamp is back in stock"), (Render("back-in-stock").To, Render("back-in-stock").Subject));
        Assert.Contains("href=\"https://shop.example/products/31\"", Render("back-in-stock").Html);
    }

    [Fact]
    public void What_a_customer_typed_is_encoded()
    {
        var email = PaidMail.Render(Paid("<script>alert(1)</script> Nowak"));

        Assert.DoesNotContain("<script>", email.Html);
        Assert.Contains("Hi &lt;script&gt;alert(1)&lt;/script&gt;,", email.Html);
    }

    [Theory]
    [InlineData(PaymentDeclineReasons.CardDeclined, "the bank declined the card")]
    [InlineData(PaymentDeclineReasons.InsufficientFunds, "the card did not have enough funds")]
    [InlineData(PaymentDeclineReasons.AuthenticationFailed, "the 3-D Secure check was not passed")]
    public void A_declined_payment_says_why_in_words(string reason, string words)
    {
        var email = DeclinedMail.Render(Declined(reason));

        Assert.Contains(words, email.Text);
        Assert.Contains("We hold your pieces until 10:30 UTC on Mon, Sep 28", email.Text);
    }

    [Fact]
    public void Smtp_delivery_needs_a_server()
    {
        var options = new MailOptions { Delivery = MailDelivery.Smtp, From = "Store <hello@store.example>" };

        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        Assert.False(Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MailOptions.SmtpHost)));
    }

    [Fact]
    public async Task The_events_become_emails()
    {
        var sent = new SentMail();
        await using var provider = new ServiceCollection()
            .AddSingleton<IMailTemplate<OrderPaid>>(PaidMail)
            .AddSingleton<IMailTemplate<PaymentDeclined>>(DeclinedMail)
            .AddSingleton<IMailTemplate<OrderShipped>>(ShippedMail)
            .AddSingleton<IMailTemplate<ProductBackInStock>>(BackInStockMail)
            .AddSingleton<IMailSender>(sent)
            .AddMassTransitTestHarness(bus =>
            {
                bus.AddConsumer<OrderPaidConsumer>();
                bus.AddConsumer<PaymentDeclinedConsumer>();
                bus.AddConsumer<OrderShippedConsumer>();
                bus.AddConsumer<ProductBackInStockConsumer>();
            })
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(Paid());
        await harness.Bus.Publish(Declined(PaymentDeclineReasons.CardDeclined));
        await harness.Bus.Publish(Shipped);
        await harness.Bus.Publish(BackInStock);

        Assert.True(await harness.Consumed.Any<ProductBackInStock>());
        Assert.True(await harness.Consumed.Any<OrderShipped>());
        Assert.True(await harness.Consumed.Any<PaymentDeclined>());
        Assert.True(await harness.Consumed.Any<OrderPaid>());
        Assert.Equal(
            new[] { "back-in-stock", "order-paid", "order-shipped", "payment-declined" },
            sent.Emails.Select(e => e.Kind).Order());
    }

    private sealed class SentMail : IMailSender
    {
        public System.Collections.Concurrent.ConcurrentBag<Email> Emails { get; } = new();

        public Task SendAsync(Email email, CancellationToken cancellationToken = default)
        {
            Emails.Add(email);
            return Task.CompletedTask;
        }
    }

    /// <summary>Compares a rendered e-mail with its file in Snapshots/; UPDATE_SNAPSHOTS=1 writes the file instead.</summary>
    private static class Snapshot
    {
        public static void Match(string name, string actual)
        {
            var path = Path.Combine(SnapshotsDirectory(), name);
            var normalized = actual.Replace("\r\n", "\n");
            if (Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1" || !File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, normalized);
                Assert.Fail($"Snapshot {name} written; check it and run the tests again");
            }

            Assert.Equal(File.ReadAllText(path).Replace("\r\n", "\n"), normalized);
        }

        // [CallerFilePath] is rewritten to "/_/" by the deterministic CI build, so the folder is found from the output directory.
        private static string SnapshotsDirectory()
        {
            var relative = Path.Combine("Tests", "Unit", "NotificationService", "Snapshots");
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new DirectoryNotFoundException($"{relative} was not found above {AppContext.BaseDirectory}");
        }
    }
}
