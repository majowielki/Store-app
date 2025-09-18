using FluentValidation;
using Store.IdentityService.DTOs.Requests;

namespace Store.IdentityService.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
            .MaximumLength(100).WithMessage("Password must be at most 100 characters.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("ConfirmPassword must match Password.");
        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("FirstName must be at most 100 characters.");
        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("LastName must be at most 100 characters.");
    }
}
