using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Store.OrderService.Live;
using Store.Tests.Integration.TestSupport;
using System.Collections.Concurrent;
using Xunit;

namespace Store.Tests.Integration.Orders;

/// <summary>
/// The admin panel's live feed of orders, connected the way the panel connects: one WebSocket,
/// no negotiate request, the administrator's token on it.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class LiveOrdersTests : IClassFixture<OrderApiFactory>
{
    private readonly OrderApiFactory _factory;
    private readonly OrderJourney _journey;

    public LiveOrdersTests(OrderApiFactory factory)
    {
        _factory = factory;
        _journey = new OrderJourney(factory);
    }

    private async Task<HubConnection> ConnectAsync(string token)
    {
        var sockets = _factory.Server.CreateWebSocketClient();
        sockets.ConfigureRequest = request => request.Headers.Authorization = $"Bearer {token}";
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, LiveOrdersHub.Path), options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.WebSocketFactory = async (context, cancellationToken) => await sockets.ConnectAsync(context.Uri, cancellationToken);
            })
            .Build();
        await connection.StartAsync();
        return connection;
    }

    [Fact]
    public async Task A_panel_hears_of_a_new_order_and_of_every_status_it_moves_to()
    {
        var heard = new ConcurrentQueue<LiveOrderUpdate>();
        await using var panel = await ConnectAsync(TestTokens.DemoAdmin("live-panel"));
        panel.On<LiveOrderUpdate>(nameof(ILiveOrdersClient.OrderChanged), heard.Enqueue);

        var orderId = await _journey.PaidAsync("live-buyer", 701);

        // Each change is its own event, so they may be told in any order
        await Eventually.AssertAsync(() =>
        {
            Assert.Equivalent(new[] { "Placed", "AwaitingPayment", "Paid" }, heard.Where(u => u.OrderId == orderId).Select(u => u.Status).ToArray());
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task A_customer_cannot_listen()
    {
        var refused = await Assert.ThrowsAnyAsync<Exception>(() => ConnectAsync(TestTokens.User("live-customer")));

        Assert.Contains("403", refused.Message, StringComparison.Ordinal);
    }
}
