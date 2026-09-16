using FluentValidation;
using Store.ProductService.DTOs.Requests;

namespace Store.ProductService.Validators;

/// <summary>
/// Same limits as on create, applied only to the fields the request carries. A field sent
/// as null passes here for the clearable ones (sale price, discount, dimensions) and fails
/// for the ones a product cannot do without.
/// </summary>
public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Title).ProductTitle().When(x => x.Title is not null);
        RuleFor(x => x.Description).ProductDescription().When(x => x.Description is not null);
        RuleFor(x => x.Price).ProductPrice("Price").When(x => x.Price.HasValue);
        RuleFor(x => x.SalePrice.Value).ProductPrice("SalePrice").When(x => x.SalePrice is { IsSet: true, Value: not null });
        RuleFor(x => x.DiscountPercent.Value).DiscountPercent().When(x => x.DiscountPercent is { IsSet: true, Value: not null });
        RuleFor(x => x.Category).IsInEnum().WithMessage("Category must be valid.").When(x => x.Category.HasValue);
        RuleFor(x => x.Company).IsInEnum().WithMessage("Company must be valid.").When(x => x.Company.HasValue);
        RuleFor(x => x.Image).ProductImage().When(x => x.Image is not null);
        RuleFor(x => x.Colors).ProductColors().When(x => x.Colors is not null);
        RuleFor(x => x.Groups).ProductGroups().When(x => x.Groups is not null);
        RuleFor(x => x.WidthCm.Value).Dimension("WidthCm").When(x => x.WidthCm is { IsSet: true, Value: not null });
        RuleFor(x => x.HeightCm.Value).Dimension("HeightCm").When(x => x.HeightCm is { IsSet: true, Value: not null });
        RuleFor(x => x.DepthCm.Value).Dimension("DepthCm").When(x => x.DepthCm is { IsSet: true, Value: not null });
        RuleFor(x => x.WeightKg.Value).Dimension("WeightKg").When(x => x.WeightKg is { IsSet: true, Value: not null });
        RuleFor(x => x.Materials).ProductMaterials().When(x => x.Materials is not null);
    }
}
