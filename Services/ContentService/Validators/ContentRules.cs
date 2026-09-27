using FluentValidation;
using Store.ContentService.Models;

namespace Store.ContentService.Validators;

/// <summary>Rules the four request validators share, written once against <see cref="ContentConstraints"/>.</summary>
internal static class ContentRules
{
    public static IRuleBuilderOptions<T, string> Slug<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty()
        .MaximumLength(ContentConstraints.SlugMaxLength)
        .Matches(ContentConstraints.SlugPattern).WithMessage("Use lowercase letters and digits in words joined by single dashes.");

    public static IRuleBuilderOptions<T, string> Title<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty()
        .Length(ContentConstraints.TitleMinLength, ContentConstraints.TitleMaxLength);

    public static IRuleBuilderOptions<T, string> Markdown<T>(this IRuleBuilder<T, string> rule) => rule
        .MaximumLength(ContentConstraints.MarkdownMaxLength);

    public static IRuleBuilderOptions<T, string> Picture<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty()
        .MaximumLength(ContentConstraints.ImageMaxLength)
        .Must(value => Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        .WithMessage("The picture must be an absolute http(s) URL.");

    public static IRuleBuilderOptions<T, IEnumerable<string>> ProductSlugs<T>(this IRuleBuilder<T, List<string>> rule) => rule
        .NotNull()
        .Must(slugs => slugs.Count <= ContentConstraints.MaxProducts).WithMessage($"At most {ContentConstraints.MaxProducts} products.")
        .Must(slugs => slugs.Distinct(StringComparer.Ordinal).Count() == slugs.Count).WithMessage("A product is listed twice.")
        .ForEach(slug => slug
            .NotEmpty()
            .MaximumLength(ContentConstraints.ProductSlugMaxLength)
            .Matches(ContentConstraints.SlugPattern).WithMessage("A product slug is lowercase letters and digits joined by dashes."));
}
