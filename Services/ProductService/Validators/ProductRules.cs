using FluentValidation;
using Store.ProductService.Models;

namespace Store.ProductService.Validators;

/// <summary>
/// Rules the create and update validators share, expressed once against <see cref="ProductConstraints"/>.
/// </summary>
internal static class ProductRules
{
    public static bool IsAbsoluteUrl(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public static IRuleBuilderOptions<T, string?> ProductTitle<T>(this IRuleBuilder<T, string?> rule) => rule
        .NotEmpty().WithMessage("Title is required.")
        .Length(ProductConstraints.TitleMinLength, ProductConstraints.TitleMaxLength)
        .WithMessage($"Title must be between {ProductConstraints.TitleMinLength} and {ProductConstraints.TitleMaxLength} characters.");

    public static IRuleBuilderOptions<T, string?> ProductDescription<T>(this IRuleBuilder<T, string?> rule) => rule
        .NotEmpty().WithMessage("Description is required.")
        .Length(ProductConstraints.DescriptionMinLength, ProductConstraints.DescriptionMaxLength)
        .WithMessage($"Description must be between {ProductConstraints.DescriptionMinLength} and {ProductConstraints.DescriptionMaxLength} characters.");

    public static IRuleBuilderOptions<T, decimal> ProductPrice<T>(this IRuleBuilder<T, decimal> rule, string name) => rule
        .InclusiveBetween(ProductConstraints.MinPrice, ProductConstraints.MaxPrice)
        .WithMessage($"{name} must be between {ProductConstraints.MinPrice} and {ProductConstraints.MaxPrice}.");

    public static IRuleBuilderOptions<T, decimal?> ProductPrice<T>(this IRuleBuilder<T, decimal?> rule, string name) => rule
        .InclusiveBetween(ProductConstraints.MinPrice, ProductConstraints.MaxPrice)
        .WithMessage($"{name} must be between {ProductConstraints.MinPrice} and {ProductConstraints.MaxPrice}.");

    public static IRuleBuilderOptions<T, decimal?> DiscountPercent<T>(this IRuleBuilder<T, decimal?> rule) => rule
        .InclusiveBetween(0, ProductConstraints.MaxDiscountPercent)
        .WithMessage($"DiscountPercent must be between 0 and {ProductConstraints.MaxDiscountPercent}.");

    public static IRuleBuilderOptions<T, decimal?> Dimension<T>(this IRuleBuilder<T, decimal?> rule, string name) => rule
        .InclusiveBetween(0, ProductConstraints.MaxDimension)
        .WithMessage($"{name} must be between 0 and {ProductConstraints.MaxDimension}.");

    public static IRuleBuilderOptions<T, string?> ProductImage<T>(this IRuleBuilder<T, string?> rule) => rule
        .NotEmpty().WithMessage("Image is required.")
        .Must(IsAbsoluteUrl).WithMessage("Image must be a valid http(s) URL.");

    public static IRuleBuilderOptions<T, IEnumerable<string>> ProductColors<T>(this IRuleBuilder<T, List<string>?> rule) => rule
        .NotNull().WithMessage("Colors are required.")
        .Must(c => c!.Count > 0).WithMessage("At least one color is required.")
        .ForEach(color => color
            .NotEmpty().WithMessage("Color cannot be empty.")
            .MaximumLength(ProductConstraints.ColorMaxLength).WithMessage($"Color must be at most {ProductConstraints.ColorMaxLength} characters."));

    public static IRuleBuilderOptions<T, IEnumerable<string>> ProductGroups<T>(this IRuleBuilder<T, List<string>?> rule) => rule
        .ForEach(group => group
            .NotEmpty().WithMessage("Group cannot be empty.")
            .MaximumLength(ProductConstraints.GroupMaxLength).WithMessage($"Group must be at most {ProductConstraints.GroupMaxLength} characters."));

    public static IRuleBuilderOptions<T, IEnumerable<string>> ProductMaterials<T>(this IRuleBuilder<T, List<string>?> rule) => rule
        .ForEach(material => material
            .NotEmpty().WithMessage("Material cannot be empty.")
            .MaximumLength(ProductConstraints.MaterialMaxLength).WithMessage($"Material must be at most {ProductConstraints.MaterialMaxLength} characters."));
}
