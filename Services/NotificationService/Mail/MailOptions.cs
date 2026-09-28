using System.ComponentModel.DataAnnotations;

namespace Store.NotificationService.Mail;

/// <summary>How the e-mails leave: written to the log (the default, and production's while the shop is a demo) or sent over SMTP.</summary>
public enum MailDelivery
{
    Log,
    Smtp
}

/// <summary>The <c>Mail</c> section: where e-mails go and who they are from; their links lead to <c>Shop:Url</c>.</summary>
public sealed class MailOptions : IValidatableObject
{
    public const string SectionName = "Mail";

    /// <summary>The standard SMTP port; Mailpit listens on 1025 instead.</summary>
    public const int DefaultSmtpPort = 25;

    public MailDelivery Delivery { get; init; } = MailDelivery.Log;

    /// <summary>"Store &lt;hello@store.example&gt;".</summary>
    [Required]
    public string From { get; init; } = string.Empty;

    /// <summary>The SMTP server (Mailpit in development); required for <see cref="MailDelivery.Smtp"/>.</summary>
    public string? SmtpHost { get; init; }

    [Range(1, 65535)]
    public int SmtpPort { get; init; } = DefaultSmtpPort;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Delivery == MailDelivery.Smtp && string.IsNullOrWhiteSpace(SmtpHost))
        {
            yield return new ValidationResult("Mail:SmtpHost is required to send over SMTP", [nameof(SmtpHost)]);
        }
    }
}
