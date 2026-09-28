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
            .Must(status => Enum.TryParse<OrderStatus>(status, ignoreCase: false, out var parsed)
                && !int.TryParse(status, out _)
                && OrderStatusFlow.IsAdministratorMove(parsed))
            .WithMessage($"Status must be {OrderStatus.Shipped} or {OrderStatus.Cancelled}; payments move an order on their own.");
    }
}
