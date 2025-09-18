using FluentValidation;
using Store.Shared.Models;

namespace Store.AuditLogService.Validators;

public class AuditLogValidator : AbstractValidator<AuditLog>
{
    public AuditLogValidator()
    {
        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.");
        RuleFor(x => x.EntityName)
            .NotEmpty().WithMessage("EntityName is required.");
        RuleFor(x => x.UserEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.UserEmail)).WithMessage("UserEmail must be a valid email address.");
        RuleFor(x => x.IpAddress)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.IpAddress)).WithMessage("IpAddress must be at most 100 characters.");
        RuleFor(x => x.UserAgent)
            .MaximumLength(300).When(x => !string.IsNullOrEmpty(x.UserAgent)).WithMessage("UserAgent must be at most 300 characters.");
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
        RuleFor(x => x.StackTrace)
            .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.StackTrace)).WithMessage("StackTrace must be at most 2000 characters.");
    }
}
