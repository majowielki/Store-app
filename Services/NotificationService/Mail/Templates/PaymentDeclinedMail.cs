using Store.BuildingBlocks.Shop;
using Store.Contracts.Payments.V1;
using System.Text;
using static Store.NotificationService.Mail.MailFormats;
using static Store.NotificationService.Mail.MailHtml;

namespace Store.NotificationService.Mail.Templates;

/// <summary>A refused card, why in words, and a link back to the payment while the order still waits.</summary>
public sealed class PaymentDeclinedMail : IMailTemplate<PaymentDeclined>
{
    public const string Kind = "payment-declined";

    private readonly ShopLinks _links;

    public PaymentDeclinedMail(ShopLinks links)
    {
        _links = links;
    }

    public Email Render(PaymentDeclined payment)
    {
        var reason = payment.Reason switch
        {
            PaymentDeclineReasons.InsufficientFunds => "the card did not have enough funds",
            PaymentDeclineReasons.AuthenticationFailed => "the 3-D Secure check was not passed",
            _ => "the bank declined the card"
        };
        var until = payment.RetryUntil is { } deadline
            ? $"We hold your pieces until {deadline.ToString(Time, English)} UTC on {deadline.ToString(Day, English)} - pay with another card before then to keep them."
            : "Pay with another card to keep your pieces.";
        var link = _links.PayOrder(payment.OrderId);
        var html = new StringBuilder()
            .Append(Paragraph($"Hi {Encode(FirstName(payment.CustomerName))},"))
            .Append(Paragraph($"Your payment for order #{payment.OrderId} did not go through: {Encode(reason)}. Nothing was charged."))
            .Append(Paragraph(Encode(until)))
            .Append(Button("Pay with another card", link))
            .ToString();
        var text = $"Hi {FirstName(payment.CustomerName)},\n\nYour payment for order #{payment.OrderId} did not go through: {reason}. Nothing was charged.\n\n{until}\n\nPay with another card: {link}\n";
        return new Email(payment.UserEmail, $"Your payment for order #{payment.OrderId} did not go through", Layout("Your payment did not go through", html), text, Kind);
    }
}
