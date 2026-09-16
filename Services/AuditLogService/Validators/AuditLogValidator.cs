using FluentValidation;
using Store.Shared.Models;

namespace Store.AuditLogService.Validators;

public class AuditLogValidator : AbstractValidator<AuditLog>
{
    public AuditLogValidator()
    {
        // Limits mirror the AuditLogs column lengths in AuditLogDbContext so nothing passes
        // validation only to fail on insert (or the other way round)
        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.")
            .MaximumLength(50).WithMessage("Action must be at most 50 characters.");
        RuleFor(x => x.EntityName)
            .NotEmpty().WithMessage("EntityName is required.")
            .MaximumLength(100).WithMessage("EntityName must be at most 100 characters.");
        RuleFor(x => x.EntityId)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.EntityId)).WithMessage("EntityId must be at most 50 characters.");
        RuleFor(x => x.UserId)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.UserId)).WithMessage("UserId must be at most 50 characters.");
        RuleFor(x => x.UserEmail)
            .MaximumLength(256).When(x => !string.IsNullOrEmpty(x.UserEmail)).WithMessage("UserEmail must be at most 256 characters.")
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.UserEmail)).WithMessage("UserEmail must be a valid email address.");
        RuleFor(x => x.IpAddress)
            .MaximumLength(45).When(x => !string.IsNullOrEmpty(x.IpAddress)).WithMessage("IpAddress must be at most 45 characters.");
        RuleFor(x => x.UserAgent)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.UserAgent)).WithMessage("UserAgent must be at most 500 characters.");
        RuleFor(x => x.ServiceName)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.ServiceName)).WithMessage("ServiceName must be at most 100 characters.");
        RuleFor(x => x.CorrelationId)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.CorrelationId)).WithMessage("CorrelationId must be at most 100 characters.");
        RuleFor(x => x.HttpMethod)
            .MaximumLength(10).When(x => !string.IsNullOrEmpty(x.HttpMethod)).WithMessage("HttpMethod must be at most 10 characters.");
        RuleFor(x => x.Path)
            .MaximumLength(300).When(x => !string.IsNullOrEmpty(x.Path)).WithMessage("Path must be at most 300 characters.");
        RuleFor(x => x.SessionId)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.SessionId)).WithMessage("SessionId must be at most 100 characters.");
    }
}
