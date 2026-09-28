using FluentValidation;
using Store.Contracts.Payments;
using Store.PaymentService.DTOs;
using Store.PaymentService.Providers;

namespace Store.PaymentService.Validators;

/// <summary>
/// A card has to look like one before it is charged: digits that pass the Luhn check, a month and
/// year still ahead, a 3-4 digit code - and in this demo it has to be a test card. No message ever
/// repeats what was typed.
/// </summary>
public class ConfirmPaymentRequestValidator : AbstractValidator<ConfirmPaymentRequest>
{
    public ConfirmPaymentRequestValidator(TimeProvider time, IPaymentProvider provider)
    {
        RuleFor(x => x.CardNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Enter the card number.")
            .Must(number => CardNumbers.Normalize(number) is { Length: >= 12 and <= 19 } digits && digits.All(char.IsAsciiDigit))
            .WithMessage("A card number has 12 to 19 digits.")
            .Must(number => CardNumbers.PassesLuhn(CardNumbers.Normalize(number)))
            .WithMessage("That card number is not valid - check the digits.")
            .Must(provider.Accepts)
            .WithMessage("This is a demo shop: pay with one of the test cards listed with the form.");

        RuleFor(x => x.ExpMonth).InclusiveBetween(1, 12).WithMessage("The expiry month is 1 to 12.");

        RuleFor(x => x.ExpYear)
            .InclusiveBetween(2000, 2100).WithMessage("Give the expiry year in four digits.")
            .Must((request, year) => !Expired(request.ExpMonth, year, time.GetUtcNow()))
            .WithMessage("The card has expired.")
            .When(x => x.ExpMonth is >= 1 and <= 12);

        RuleFor(x => x.Cvc)
            .Matches("^[0-9]{3,4}$").WithMessage("The security code is the 3 or 4 digits on the back of the card.");
    }

    /// <summary>A card works until the end of its expiry month.</summary>
    private static bool Expired(int month, int year, DateTimeOffset now)
        => year < now.Year || (year == now.Year && month < now.Month);
}

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.Currency).Equal("usd").WithMessage("The shop charges in usd.");
    }
}
