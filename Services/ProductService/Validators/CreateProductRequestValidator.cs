using FluentValidation;
using Store.ProductService.DTOs.Requests;

namespace Store.ProductService.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .Length(3, 200).WithMessage("Title must be between 3 and 200 characters.");
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .Length(10, 4000).WithMessage("Description must be between 10 and 4000 characters.");
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0.01m).WithMessage("Price must be at least 0.01.")
            .LessThanOrEqualTo(999999.99m).WithMessage("Price must be less than or equal to 999999.99.");
        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0.01m).When(x => x.SalePrice.HasValue).WithMessage("SalePrice must be at least 0.01.")
            .LessThanOrEqualTo(999999.99m).When(x => x.SalePrice.HasValue).WithMessage("SalePrice must be less than or equal to 999999.99.");
        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0, 100).When(x => x.DiscountPercent.HasValue).WithMessage("DiscountPercent must be between 0 and 100.");
        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Category is required and must be valid.");
        RuleFor(x => x.Company)
            .IsInEnum().WithMessage("Company is required and must be valid.");
        RuleFor(x => x.Image)
            .NotEmpty().WithMessage("Image is required.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("Image must be a valid URL.");
        RuleFor(x => x.Colors)
            .NotNull().WithMessage("Colors are required.")
            .Must(c => c.Count > 0).WithMessage("At least one color is required.");
        RuleForEach(x => x.Colors)
            .NotEmpty().WithMessage("Color cannot be empty.")
            .MaximumLength(50).WithMessage("Color must be at most 50 characters.");
        RuleForEach(x => x.Groups)
            .MaximumLength(100).WithMessage("Group must be at most 100 characters.");
        RuleFor(x => x.WidthCm).InclusiveBetween(0, 100000).When(x => x.WidthCm.HasValue);
        RuleFor(x => x.HeightCm).InclusiveBetween(0, 100000).When(x => x.HeightCm.HasValue);
        RuleFor(x => x.DepthCm).InclusiveBetween(0, 100000).When(x => x.DepthCm.HasValue);
        RuleFor(x => x.WeightKg).InclusiveBetween(0, 100000).When(x => x.WeightKg.HasValue);
        RuleForEach(x => x.Materials)
            .MaximumLength(100).WithMessage("Material must be at most 100 characters.");
    }
}
