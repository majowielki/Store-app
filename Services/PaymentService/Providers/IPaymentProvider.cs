namespace Store.PaymentService.Providers;

/// <summary>A card as the customer typed it. Lives only for the request; never stored or logged.</summary>
public sealed record CardDetails(string Number, int ExpMonth, int ExpYear, string Cvc)
{
    // A record prints its members; a card must not end up in a log that way
    public override string ToString() => $"card ending {CardNumbers.Summarize(Number).Last4}";
}

/// <summary>What the card network answers to a charge; the API writes it in camelCase.</summary>
public enum ChargeResult
{
    Approved,
    /// <summary>The bank wants the customer's 3-D Secure approval first.</summary>
    AuthenticationRequired,
    Declined,
    InsufficientFunds
}

/// <summary>A card number the provider takes in test mode, and what charging it does.</summary>
/// <param name="Number">The number in groups of four, as printed on a card</param>
/// <param name="Outcome">What a charge of it answers</param>
public sealed record TestCard(string Number, ChargeResult Outcome);

/// <summary>
/// The card network behind the payment service (ADR 011). The demo charges no real card: it runs
/// on <see cref="TestCardProvider"/>; a real provider in test mode would go behind this interface.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>The cards the provider takes in test mode (the payment page lists them); none in live mode.</summary>
    IReadOnlyList<TestCard> TestCards { get; }

    /// <summary>Whether the provider will take this number at all.</summary>
    bool Accepts(string cardNumber);

    ChargeResult Charge(CardDetails card, decimal amount);
}
