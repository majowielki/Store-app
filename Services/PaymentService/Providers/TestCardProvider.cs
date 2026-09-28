namespace Store.PaymentService.Providers;

/// <summary>
/// The test cards of the analysis, numbered like Stripe's test mode: 4242 4242 4242 4242 pays,
/// 4000 0000 0000 3220 asks for 3-D Secure, 4000 0000 0000 9995 has no funds, 4000 0000 0000 0002
/// is declined. Any other number is refused before it is charged, so nobody can type a real card
/// into the demo.
/// </summary>
public sealed class TestCardProvider : IPaymentProvider
{
    public const string Succeeds = "4242 4242 4242 4242";
    public const string RequiresAuthentication = "4000 0000 0000 3220";
    public const string InsufficientFunds = "4000 0000 0000 9995";
    public const string Declined = "4000 0000 0000 0002";

    private static readonly IReadOnlyList<TestCard> Cards =
    [
        new(Succeeds, ChargeResult.Approved),
        new(RequiresAuthentication, ChargeResult.AuthenticationRequired),
        new(InsufficientFunds, ChargeResult.InsufficientFunds),
        new(Declined, ChargeResult.Declined)
    ];

    private static readonly Dictionary<string, ChargeResult> Outcomes =
        Cards.ToDictionary(card => CardNumbers.Normalize(card.Number), card => card.Outcome);

    public IReadOnlyList<TestCard> TestCards => Cards;

    public bool Accepts(string cardNumber) => Outcomes.ContainsKey(CardNumbers.Normalize(cardNumber));

    public ChargeResult Charge(CardDetails card, decimal amount)
        => Outcomes.TryGetValue(CardNumbers.Normalize(card.Number), out var result)
            ? result
            : throw new InvalidOperationException("Only the test cards can be charged.");
}
