using Store.BuildingBlocks.Authorization;
using Store.OrderService.DTOs.Responses;
using System.Security.Claims;

namespace Store.OrderService.Controllers;

/// <summary>
/// The demo administrator may open every admin view but must not see customers' personal
/// data: orders are handed out with placeholders instead of the customer fields.
/// </summary>
public static class OrderMasking
{
    public const string AnonymizedUserId = "anonymized-user-id";
    public const string AnonymizedUserEmail = "anonymized-user-email";
    public const string AnonymizedDeliveryAddress = "anonymized-delivery-address";
    public const string AnonymizedCustomerName = "anonymized-customer-name";

    public static OrderResponse ForViewer(this OrderResponse order, ClaimsPrincipal viewer)
    {
        if (!viewer.IsDemoAdmin())
        {
            return order;
        }

        order.UserId = AnonymizedUserId;
        order.UserEmail = AnonymizedUserEmail;
        order.DeliveryAddress = AnonymizedDeliveryAddress;
        order.CustomerName = AnonymizedCustomerName;
        return order;
    }

    public static OrderListResponse ForViewer(this OrderListResponse list, ClaimsPrincipal viewer)
    {
        if (viewer.IsDemoAdmin())
        {
            list.Orders = list.Orders.Select(order => order.ForViewer(viewer)).ToList();
        }

        return list;
    }
}
