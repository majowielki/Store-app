using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.Validators;
using Xunit;

namespace Store.Tests.Unit.IdentityService;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        var model = new RegisterRequest { Email = "", Password = "Password123", ConfirmPassword = "Password123" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Invalid()
    {
        var model = new RegisterRequest { Email = "not-an-email", Password = "Password123", ConfirmPassword = "Password123" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Too_Short()
    {
        var model = new RegisterRequest { Email = "test@example.com", Password = "123", ConfirmPassword = "123" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void Should_Have_Error_When_ConfirmPassword_Does_Not_Match()
    {
        var model = new RegisterRequest { Email = "test@example.com", Password = "Password123", ConfirmPassword = "Different123" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.ConfirmPassword));
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Model()
    {
        var model = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123",
            ConfirmPassword = "Password123",
            FirstName = "John",
            LastName = "Doe"
        };
        var result = _validator.Validate(model);
        Assert.True(result.IsValid);
    }
}
