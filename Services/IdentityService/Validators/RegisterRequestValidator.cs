using FluentValidation;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.Models;

namespace Store.IdentityService.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(ApplicationUser.EmailMaxLength).WithMessage($"Email must be at most {ApplicationUser.EmailMaxLength} characters.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordPolicy.MinLength).WithMessage($"Password must be at least {PasswordPolicy.MinLength} characters.")
            .MaximumLength(PasswordPolicy.MaxLength).WithMessage($"Password must be at most {PasswordPolicy.MaxLength} characters.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("ConfirmPassword must match Password.");
        RuleFor(x => x.FirstName)
            .MaximumLength(ApplicationUser.NameMaxLength).WithMessage($"FirstName must be at most {ApplicationUser.NameMaxLength} characters.");
        RuleFor(x => x.LastName)
            .MaximumLength(ApplicationUser.NameMaxLength).WithMessage($"LastName must be at most {ApplicationUser.NameMaxLength} characters.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class UpdateAddressRequestValidator : AbstractValidator<UpdateAddressRequest>
{
    public UpdateAddressRequestValidator()
    {
        RuleFor(x => x.SimpleAddress)
            .MaximumLength(ApplicationUser.AddressMaxLength).WithMessage($"Address must be at most {ApplicationUser.AddressMaxLength} characters.");
    }
}
