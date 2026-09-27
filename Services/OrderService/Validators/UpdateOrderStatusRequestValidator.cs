using FluentValidation;
using Store.OrderService.DTOs.Requests;
using Store.OrderService.Models;

namespace Store.OrderService.Validators;

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status => Enum.TryParse<OrderStatus>(status, ignoreCase: false, out _) && !int.TryParse(status, out _))
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<OrderStatus>())}.");
    }
}
