using Store.OrderService.Live;
using Store.OrderService.Models;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Store.Tests.Unit.OrderService;

public sealed class LiveContractTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Fact]
    public void Browser_protocol_matches_the_server_path_message_payload_and_statuses()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Store.Microservices.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var ui = Path.Combine(root.FullName, "UI", "store-app.UI", "src");
        var live = File.ReadAllText(Path.Combine(ui, "features", "live", "liveOrders.ts"));
        Assert.Contains($"LIVE_ORDERS_PATH = '{LiveOrdersHub.Path["/api/v1".Length..]}'", live);
        Assert.Contains($"ORDER_CHANGED = '{nameof(ILiveOrdersClient.OrderChanged)}'", live);
        var payload = JsonSerializer.SerializeToElement(new LiveOrderUpdate(1, nameof(OrderStatus.Paid), DateTime.UtcNow), Json);
        var fields = Regex.Match(live, @"interface LiveOrderUpdate\s*\{([^}]+)\}").Groups[1].Value;
        var declarations = Regex.Matches(fields, @"(\w+):\s*(\w+)").ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        Assert.Equal(payload.EnumerateObject().Select(p => p.Name).Order(), declarations.Keys.Order());
        Assert.Equal("number", declarations["orderId"]);
        Assert.Equal("OrderStatus", declarations["status"]);
        Assert.Equal("string", declarations["at"]);
        var types = File.ReadAllText(Path.Combine(ui, "api", "types.ts"));
        var statuses = Regex.Match(types, @"type OrderStatus = ([^;]+);").Groups[1].Value;
        Assert.Equal(Enum.GetNames<OrderStatus>().Order(), Regex.Matches(statuses, "'([^']+)'").Select(m => m.Groups[1].Value).Order());
    }
}
