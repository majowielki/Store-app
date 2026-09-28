using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Store.Contracts.Orders.V1;
using Store.NotificationService.Mail;
using Store.Tests.Integration.TestSupport;
using System.Collections.Concurrent;
using System.Net;
using Xunit;

namespace Store.Tests.Integration.Notifications;

/// <summary>The notification service as it starts: no database, the bus in memory, the e-mails caught instead of sent.</summary>
public sealed class NotificationApiFactory : StoreApiFactory<ShopLinks>
{
    public NotificationApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    public CaughtMail Mail { get; } = new();

    protected override string? DatabaseName => null;

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<IMailSender>();
        services.AddSingleton<IMailSender>(Mail);
    }
}

public sealed class CaughtMail : IMailSender
{
    public ConcurrentQueue<Email> Emails { get; } = new();

    public Task SendAsync(Email email, CancellationToken cancellationToken = default)
    {
        Emails.Enqueue(email);
        return Task.CompletedTask;
    }
}

[Collection(PostgresTests.Name)]
public sealed class NotificationTests : IClassFixture<NotificationApiFactory>
{
    private readonly NotificationApiFactory _factory;

    public NotificationTests(NotificationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task The_service_starts_without_a_database_and_answers_its_health_checks()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task A_paid_order_is_confirmed_by_email_to_its_customer()
    {
        await _factory.Bus.Bus.Publish(new OrderPaid(
            9001, "user-9", "buyer@test.local", "Ewa Kowalska", 120m, "mastercard", "5454",
            [new OrderItem(3, "Ceramic Table Lamp", 1, 120m)], null, null, DateTime.UtcNow));

        Assert.True(await Eventually.BecomesTrueAsync(() => _factory.Mail.Emails.Any(e => e.Subject == "Thank you for your order #9001")));
        var email = _factory.Mail.Emails.Single(e => e.Subject == "Thank you for your order #9001");
        Assert.Equal("buyer@test.local", email.To);
        Assert.Contains("Mastercard •••• 5454", email.Text);
        Assert.Contains("http://localhost:8081/orders/9001", email.Text);
    }
}
