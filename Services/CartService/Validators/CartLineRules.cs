using FluentValidation;

namespace Store.CartService.Validators;

/// <summary>Limits a cart line must respect, shared by every request that carries one.</summary>
internal static class CartLineRules
{
    public const int MaxQuantity = 999;
    public const int ColorMaxLength = 50;

    public static IRuleBuilderOptions<T, int> ProductId<T>(this IRuleBuilder<T, int> rule) => rule
        .GreaterThan(0).WithMessage("ProductId is required and must be greater than 0.");

    public static IRuleBuilderOptions<T, int> Quantity<T>(this IRuleBuilder<T, int> rule) => rule
        .InclusiveBetween(1, MaxQuantity).WithMessage($"Quantity must be between 1 and {MaxQuantity}.");

    public static IRuleBuilderOptions<T, int?> Quantity<T>(this IRuleBuilder<T, int?> rule) => rule
        .InclusiveBetween(1, MaxQuantity).WithMessage($"Quantity must be between 1 and {MaxQuantity}.");

    public static IRuleBuilderOptions<T, string?> Color<T>(this IRuleBuilder<T, string?> rule) => rule
        .NotEmpty().WithMessage("Color is required.")
        .MaximumLength(ColorMaxLength).WithMessage($"Color must be at most {ColorMaxLength} characters.");
}
