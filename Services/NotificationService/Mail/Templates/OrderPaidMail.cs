using Store.BuildingBlocks.Shop;
using Store.Contracts.Orders.V1;
using System.Text;
using static Store.NotificationService.Mail.MailFormats;
using static Store.NotificationService.Mail.MailHtml;

namespace Store.NotificationService.Mail.Templates;

/// <summary>The order confirmation, once the payment has arrived: the lines, the total, the card and when the pieces arrive.</summary>
public sealed class OrderPaidMail : IMailTemplate<OrderPaid>
{
    public const string Kind = "order-paid";

    private readonly ShopLinks _links;

    public OrderPaidMail(ShopLinks links)
    {
        _links = links;
    }

    public Email Render(OrderPaid order)
    {
        var card = order.CardLast4 is null ? string.Empty : $" ({CardName(order.CardBrand)} •••• {order.CardLast4})";
        var arrival = Window(order.DeliveryFrom, order.DeliveryTo);
        var link = _links.Order(order.OrderId);
        var html = new StringBuilder()
            .Append(Paragraph($"Hi {Encode(FirstName(order.CustomerName))},"))
            .Append(Paragraph($"We have received your payment of {Money(order.Total)}{Encode(card)}. Your order #{order.OrderId} is being prepared."))
            .Append(Lines(order.Lines))
            .Append(Total("Paid", order.Total))
            .Append(arrival is null ? string.Empty : Paragraph($"It arrives {Encode(arrival)}."))
            .Append(Button("View your order", link))
            .ToString();
        var text = new StringBuilder()
            .AppendLine($"Hi {FirstName(order.CustomerName)},")
            .AppendLine()
            .AppendLine($"We have received your payment of {Money(order.Total)}{card}. Your order #{order.OrderId} is being prepared.")
            .AppendLine()
            .Append(TextLines(order.Lines))
            .AppendLine($"Paid: {Money(order.Total)}")
            .AppendLine(arrival is null ? string.Empty : $"It arrives {arrival}.")
            .AppendLine($"View your order: {link}")
            .ToString();
        return new Email(order.UserEmail, $"Thank you for your order #{order.OrderId}", Layout("Thank you for your order", html), text, Kind);
    }
}
