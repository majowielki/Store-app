using FluentValidation;
using Store.OrderService.DTOs.Requests;

namespace Store.OrderService.Validators;

public class CreateOrderFromCartRequestValidator : AbstractValidator<CreateOrderFromCartRequest>
{
    public CreateOrderFromCartRequestValidator()
    {
        RuleFor(x => x.DeliveryAddress)
            .MaximumLength(300).WithMessage("DeliveryAddress must be at most 300 characters.");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("CustomerName is required.")
            .MinimumLength(2).WithMessage("CustomerName must be at least 2 characters.")
            .MaximumLength(100).WithMessage("CustomerName must be at most 100 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must be at most 500 characters.");
    }
}
