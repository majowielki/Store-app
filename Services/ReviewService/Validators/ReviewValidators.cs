using FluentValidation;
using Store.ReviewService.DTOs;
using Store.ReviewService.Models;
using Store.ReviewService.Services;

namespace Store.ReviewService.Validators;

/// <summary>The automatic checks a review passes before the administrator reads it (ADR 012).</summary>
public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Choose the product to review.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(ReviewConstraints.MinRating, ReviewConstraints.MaxRating)
            .WithMessage($"Give the product {ReviewConstraints.MinRating} to {ReviewConstraints.MaxRating} stars.");

        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .Must(title => title!.Trim().Length <= ReviewConstraints.TitleMaxLength)
            .WithMessage($"Keep the title under {ReviewConstraints.TitleMaxLength} characters.")
            .Must(title => ReviewContentRules.Problem(title) is null)
            .WithMessage((_, title) => ReviewContentRules.Problem(title))
            .When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(x => x.Body)
            .Cascade(CascadeMode.Stop)
            .Must(body => (body?.Trim().Length ?? 0) >= ReviewConstraints.BodyMinLength)
            .WithMessage($"Tell us a little more - a review has at least {ReviewConstraints.BodyMinLength} characters.")
            .Must(body => body.Trim().Length <= ReviewConstraints.BodyMaxLength)
            .WithMessage($"A review has at most {ReviewConstraints.BodyMaxLength} characters.")
            .Must(body => ReviewContentRules.Problem(body) is null)
            .WithMessage((_, body) => ReviewContentRules.Problem(body));
    }
}

public class ReportReviewRequestValidator : AbstractValidator<ReportReviewRequest>
{
    public ReportReviewRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(ReviewConstraints.ReasonMaxLength)
            .WithMessage($"Keep the reason under {ReviewConstraints.ReasonMaxLength} characters.");
    }
}

public class ModerateReviewsRequestValidator : AbstractValidator<ModerateReviewsRequest>
{
    public const int MaxBatch = 100;

    public ModerateReviewsRequestValidator()
    {
        RuleFor(x => x.Ids)
            .NotEmpty().WithMessage("Choose at least one review.")
            .Must(ids => ids.Count <= MaxBatch).WithMessage($"Moderate at most {MaxBatch} reviews at once.");

        RuleFor(x => x.Decision).IsInEnum();

        RuleFor(x => x.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithMessage("Say why the review is rejected - its author will see it.")
            .When(x => x.Decision == ModerationDecision.Reject);

        RuleFor(x => x.Reason)
            .MaximumLength(ReviewConstraints.ReasonMaxLength)
            .WithMessage($"Keep the reason under {ReviewConstraints.ReasonMaxLength} characters.");
    }
}
