using FluentValidation;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.Models;

namespace Store.OrderService.Validators;

public class DiscountCodeRequestValidator : AbstractValidator<DiscountCodeRequest>
{
    public DiscountCodeRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .Length(3, DiscountCode.MaxCodeLength).WithMessage($"Code must be 3 to {DiscountCode.MaxCodeLength} characters.")
            .Matches("^[A-Za-z0-9-]+$").WithMessage("Code may hold letters, digits and dashes only.");

        RuleFor(x => x.Kind)
            .Must(kind => kind is nameof(DiscountKind.Percent) or nameof(DiscountKind.Amount))
            .WithMessage("Kind must be Percent or Amount.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Value must be greater than 0.")
            .LessThanOrEqualTo(100).When(x => x.Kind == nameof(DiscountKind.Percent), ApplyConditionTo.CurrentValidator).WithMessage("A percentage can be at most 100.")
            .LessThanOrEqualTo(100_000).WithMessage("Value must be at most 100000.");

        RuleFor(x => x.MinimumSubtotal)
            .GreaterThan(0).When(x => x.MinimumSubtotal.HasValue).WithMessage("MinimumSubtotal must be greater than 0.");

        RuleFor(x => x.UsageLimit)
            .GreaterThan(0).When(x => x.UsageLimit.HasValue).WithMessage("UsageLimit must be at least 1.");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(x => x.StartsAt).When(x => x.StartsAt.HasValue && x.ExpiresAt.HasValue).WithMessage("ExpiresAt must be after StartsAt.");
    }
}
