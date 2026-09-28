using FluentValidation;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.Models;

namespace Store.OrderService.Validators;

public class CreateOrderFromCartRequestValidator : AbstractValidator<CreateOrderFromCartRequest>
{
    public CreateOrderFromCartRequestValidator()
    {
        RuleFor(x => x.DeliveryAddress)
            .MaximumLength(OrderConstraints.DeliveryAddressMaxLength)
            .WithMessage($"DeliveryAddress must be at most {OrderConstraints.DeliveryAddressMaxLength} characters.");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("CustomerName is required.")
            .MinimumLength(OrderConstraints.CustomerNameMinLength)
            .WithMessage($"CustomerName must be at least {OrderConstraints.CustomerNameMinLength} characters.")
            .MaximumLength(OrderConstraints.CustomerNameMaxLength)
            .WithMessage($"CustomerName must be at most {OrderConstraints.CustomerNameMaxLength} characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(OrderConstraints.NotesMaxLength).WithMessage($"Notes must be at most {OrderConstraints.NotesMaxLength} characters.");

        RuleFor(x => x.DiscountCode)
            .MaximumLength(DiscountCode.MaxCodeLength).WithMessage($"DiscountCode must be at most {DiscountCode.MaxCodeLength} characters.");
    }
}
