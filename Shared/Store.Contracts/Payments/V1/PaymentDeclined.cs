namespace Store.Contracts.Payments.V1;

/// <summary>
/// Published by the order service after a verified webhook from the payment service says a
/// card was refused. The order keeps waiting for a payment until its deadline, so the customer
/// can try another card. Consumer: the notification service (an e-mail with a link back to the
/// payment). Published through the outbox, so it is delivered at least once.
/// </summary>
/// <param name="PaymentId">Id of the payment in the payment service</param>
/// <param name="OrderId">Order the payment is for</param>
/// <param name="UserId">Customer the order belongs to</param>
/// <param name="UserEmail">Customer e-mail as given at checkout</param>
/// <param name="CustomerName">Name as given at checkout</param>
/// <param name="Reason">One of <see cref="PaymentDeclineReasons"/></param>
/// <param name="RetryUntil">Deadline for paying the order with another card (UTC)</param>
/// <param name="DeclinedAt">When the card was refused (UTC)</param>
public sealed record PaymentDeclined(
    Guid PaymentId,
    int OrderId,
    string UserId,
    string UserEmail,
    string CustomerName,
    string Reason,
    DateTime? RetryUntil,
    DateTime DeclinedAt);

/// <summary>Why a card was refused, as <see cref="PaymentDeclined.Reason"/> and the payment webhooks carry it.</summary>
public static class PaymentDeclineReasons
{
    /// <summary>The longest reason a service stores.</summary>
    public const int MaxLength = 50;

    /// <summary>The bank refused the card.</summary>
    public const string CardDeclined = "card-declined";

    /// <summary>Not enough money on the card.</summary>
    public const string InsufficientFunds = "insufficient-funds";

    /// <summary>The customer did not pass the 3-D Secure check.</summary>
    public const string AuthenticationFailed = "authentication-failed";
}
