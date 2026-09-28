using FluentValidation;
using Store.ProductService.DTOs.Requests;
using Store.ProductService.Models;

namespace Store.ProductService.Validators;

public class SetStockRequestValidator : AbstractValidator<SetStockRequest>
{
    public SetStockRequestValidator()
    {
        RuleFor(x => x.StockQuantity).StockQuantity();
    }
}

public class StockAlertRequestValidator : AbstractValidator<StockAlertRequest>
{
    public StockAlertRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Say where to write.")
            .MaximumLength(ProductConstraints.EmailMaxLength)
            .EmailAddress().WithMessage("That does not look like an e-mail address.");
    }
}
