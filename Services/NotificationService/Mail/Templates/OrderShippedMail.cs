using Store.BuildingBlocks.Shop;
using Store.Contracts.Orders.V1;
using System.Text;
using static Store.NotificationService.Mail.MailFormats;
using static Store.NotificationService.Mail.MailHtml;

namespace Store.NotificationService.Mail.Templates;

/// <summary>The order has left the workshop: its lines and when they arrive.</summary>
public sealed class OrderShippedMail : IMailTemplate<OrderShipped>
{
    public const string Kind = "order-shipped";

    private readonly ShopLinks _links;

    public OrderShippedMail(ShopLinks links)
    {
        _links = links;
    }

    public Email Render(OrderShipped order)
    {
        var arrival = Window(order.DeliveryFrom, order.DeliveryTo);
        var link = _links.Order(order.OrderId);
        var html = new StringBuilder()
            .Append(Paragraph($"Hi {Encode(FirstName(order.CustomerName))},"))
            .Append(Paragraph($"Your order #{order.OrderId} has left our workshop{(arrival is null ? "." : $" and arrives {Encode(arrival)}.")}"))
            .Append(Lines(order.Lines))
            .Append(Button("Follow your order", link))
            .ToString();
        var text = new StringBuilder()
            .AppendLine($"Hi {FirstName(order.CustomerName)},")
            .AppendLine()
            .AppendLine($"Your order #{order.OrderId} has left our workshop{(arrival is null ? "." : $" and arrives {arrival}.")}")
            .AppendLine()
            .Append(TextLines(order.Lines))
            .AppendLine($"Follow your order: {link}")
            .ToString();
        return new Email(order.UserEmail, $"Your order #{order.OrderId} is on its way", Layout("Your order is on its way", html), text, Kind);
    }
}
