using FluentValidation;
using Store.ProductService.DTOs.Requests;

namespace Store.ProductService.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Title).ProductTitle();
        RuleFor(x => x.Description).ProductDescription();
        RuleFor(x => x.Price).ProductPrice("Price");
        RuleFor(x => x.SalePrice).ProductPrice("SalePrice").When(x => x.SalePrice.HasValue);
        RuleFor(x => x.DiscountPercent).DiscountPercent().When(x => x.DiscountPercent.HasValue);
        RuleFor(x => x.Category).IsInEnum().WithMessage("Category is required and must be valid.");
        RuleFor(x => x.Company).IsInEnum().WithMessage("Company is required and must be valid.");
        RuleFor(x => x.Image).ProductImage();
        RuleFor(x => x.Colors).ProductColors();
        RuleFor(x => x.Groups).ProductGroups();
        RuleFor(x => x.WidthCm).Dimension("WidthCm").When(x => x.WidthCm.HasValue);
        RuleFor(x => x.HeightCm).Dimension("HeightCm").When(x => x.HeightCm.HasValue);
        RuleFor(x => x.DepthCm).Dimension("DepthCm").When(x => x.DepthCm.HasValue);
        RuleFor(x => x.WeightKg).Dimension("WeightKg").When(x => x.WeightKg.HasValue);
        RuleFor(x => x.Materials).ProductMaterials();
    }
}
