using FluentValidation;
using Store.CartService.DTOs.Requests;
using Store.CartService.Models;

namespace Store.CartService.Validators;

public class AddWishlistItemRequestValidator : AbstractValidator<AddWishlistItemRequest>
{
    public AddWishlistItemRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("ProductId must be greater than 0.");
    }
}

public class SyncWishlistRequestValidator : AbstractValidator<SyncWishlistRequest>
{
    public SyncWishlistRequestValidator()
    {
        RuleFor(x => x.ProductIds)
            .Must(ids => ids.Count <= WishlistItem.MaxItems).WithMessage($"A wishlist holds at most {WishlistItem.MaxItems} products.");
        RuleForEach(x => x.ProductIds).GreaterThan(0).WithMessage("Every product id must be greater than 0.");
    }
}
