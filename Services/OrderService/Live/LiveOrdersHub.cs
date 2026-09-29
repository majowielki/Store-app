using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Store.Contracts.Authorization;

namespace Store.OrderService.Live;

/// <summary>
/// The admin panel's live feed of orders: a new order and every change of status, while the panel
/// is open, without asking again. Only the server speaks; the panel listens. The feed says which
/// order changed and to what, nothing about the customer - the panel reads the order through the
/// API, where the demo administrator's masking applies.
/// </summary>
[Authorize(Policy = Policies.Admin)]
public sealed class LiveOrdersHub : Hub<ILiveOrdersClient>
{
    /// <summary>Where the hub is mapped: under the gateway's admin orders route, which needs an administrator.</summary>
    public const string Path = "/api/v1/admin/orders/live";
}

/// <summary>What the hub sends to the panels; a method's name is the message's name on the wire.</summary>
public interface ILiveOrdersClient
{
    /// <summary>An order was placed or moved to another status.</summary>
    Task OrderChanged(LiveOrderUpdate update);
}

/// <summary>One change of an order.</summary>
/// <param name="OrderId">The order</param>
/// <param name="Status">The status it entered, named as the API names it</param>
/// <param name="At">When (UTC)</param>
public sealed record LiveOrderUpdate(int OrderId, string Status, DateTime At);
