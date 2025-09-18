using FluentValidation;
using Store.CartService.DTOs.Requests;

namespace Store.CartService.Validators;

public class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("ProductId is required and must be greater than 0.");
        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 999).WithMessage("Quantity must be between 1 and 999.");
        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .MaximumLength(50).WithMessage("Color must be at most 50 characters.");
    }
}
