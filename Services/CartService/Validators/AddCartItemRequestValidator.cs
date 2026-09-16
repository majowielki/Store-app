using FluentValidation;
using Store.CartService.DTOs.Requests;

namespace Store.CartService.Validators;

public class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(x => x.ProductId).ProductId();
        RuleFor(x => x.Quantity).Quantity();
        RuleFor(x => x.Color).Color();
    }
}

public class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemRequestValidator()
    {
        RuleFor(x => x.Quantity).Quantity().When(x => x.Quantity.HasValue);
        RuleFor(x => x.Color).Color().When(x => x.Color is not null);
    }
}

public class SyncCartRequestValidator : AbstractValidator<SyncCartRequest>
{
    public SyncCartRequestValidator()
    {
        RuleFor(x => x.Items).NotNull().WithMessage("Items are required.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).ProductId();
            item.RuleFor(x => x.Quantity).Quantity();
            item.RuleFor(x => x.Color).Color();
        });
    }
}
