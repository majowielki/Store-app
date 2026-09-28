using FluentValidation;
using Store.ProductService.DTOs.Responses;
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

    public static IRuleBuilderOptions<T, int> StockQuantity<T>(this IRuleBuilder<T, int> rule) => rule
        .InclusiveBetween(0, ProductConstraints.MaxStockQuantity)
        .WithMessage($"The stock must be between 0 and {ProductConstraints.MaxStockQuantity} units.");

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

    public static IRuleBuilderOptions<T, IEnumerable<ProductImageDto>> ProductGallery<T>(this IRuleBuilder<T, List<ProductImageDto>?> rule) => rule
        .Must(images => images!.Count <= ProductConstraints.MaxGalleryImages)
        .WithMessage($"At most {ProductConstraints.MaxGalleryImages} pictures besides the main one.")
        .ForEach(image => image.ChildRules(picture =>
        {
            picture.RuleFor(p => p.Url)
                .NotEmpty().WithMessage("A gallery picture needs a URL.")
                .MaximumLength(ProductConstraints.ImageUrlMaxLength)
                .Must(IsAbsoluteUrl).WithMessage("A gallery picture must be a valid http(s) URL.");
            picture.RuleFor(p => p.Alt)
                .NotEmpty().WithMessage("Say what the picture shows.")
                .MaximumLength(ProductConstraints.ImageAltMaxLength);
        }));

    public static IRuleBuilderOptions<T, IEnumerable<ProductHotspotDto>> ProductHotspots<T>(this IRuleBuilder<T, List<ProductHotspotDto>?> rule) => rule
        .Must(points => points!.Count <= ProductConstraints.MaxHotspots)
        .WithMessage($"At most {ProductConstraints.MaxHotspots} points.")
        .ForEach(hotspot => hotspot.ChildRules(point =>
        {
            point.RuleFor(p => p.X).InclusiveBetween(0, 100);
            point.RuleFor(p => p.Y).InclusiveBetween(0, 100);
            point.RuleFor(p => p.ProductSlug)
                .NotEmpty()
                .MaximumLength(ProductConstraints.SlugMaxLength)
                .Matches(ProductConstraints.SlugPattern).WithMessage("A point must name a product by its slug.");
        }));

    public static IRuleBuilderOptions<T, IEnumerable<string>> ProductMaterials<T>(this IRuleBuilder<T, List<string>?> rule) => rule
        .ForEach(material => material
            .NotEmpty().WithMessage("Material cannot be empty.")
            .MaximumLength(ProductConstraints.MaterialMaxLength).WithMessage($"Material must be at most {ProductConstraints.MaterialMaxLength} characters."));
}
