using FluentValidation;
using Xunit;
using Store.AuditLogService.Validators;
using Store.Shared.Models;

namespace Store.Tests.Unit.AuditLogService;

public class AuditLogValidatorTests
{
    private readonly AuditLogValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Action_Is_Empty()
    {
        var model = new AuditLog { Action = "", EntityName = "Entity" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AuditLog.Action));
    }

    [Fact]
    public void Should_Have_Error_When_EntityName_Is_Empty()
    {
        var model = new AuditLog { Action = "Action", EntityName = "" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AuditLog.EntityName));
    }

    [Fact]
    public void Should_Have_Error_When_UserEmail_Invalid()
    {
        var model = new AuditLog { Action = "Action", EntityName = "Entity", UserEmail = "not-an-email" };
        var result = _validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AuditLog.UserEmail));
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Model()
    {
        var model = new AuditLog { Action = "Action", EntityName = "Entity", UserEmail = "a@b.com" };
        var result = _validator.Validate(model);
        Assert.True(result.IsValid);
    }
}
