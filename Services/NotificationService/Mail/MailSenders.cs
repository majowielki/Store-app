using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace Store.NotificationService.Mail;

/// <summary>How the e-mails leave: written to the log (the default, and production's while the shop is a demo) or sent over SMTP.</summary>
public enum MailDelivery
{
    Log,
    Smtp
}

/// <summary>The <c>Mail</c> section: where e-mails go, who they are from and where their links lead.</summary>
public sealed class MailOptions : IValidatableObject
{
    public const string SectionName = "Mail";

    public MailDelivery Delivery { get; init; } = MailDelivery.Log;

    /// <summary>"Store &lt;hello@store.example&gt;".</summary>
    [Required]
    public string From { get; init; } = string.Empty;

    /// <summary>The shop's address; the e-mails link to its order and product pages.</summary>
    [Required]
    [Url]
    public string ShopUrl { get; init; } = string.Empty;

    /// <summary>The SMTP server (Mailpit in development); required for <see cref="MailDelivery.Smtp"/>.</summary>
    public string? SmtpHost { get; init; }

    public int SmtpPort { get; init; } = 25;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Delivery == MailDelivery.Smtp && string.IsNullOrWhiteSpace(SmtpHost))
        {
            yield return new ValidationResult("Mail:SmtpHost is required to send over SMTP", [nameof(SmtpHost)]);
        }
    }
}

/// <summary>An e-mail ready to send.</summary>
/// <param name="To">The recipient's address</param>
/// <param name="Subject">The subject line</param>
/// <param name="Html">The HTML body</param>
/// <param name="Text">The plain-text body, for clients that show no HTML</param>
/// <param name="Kind">What it is about, for the logs ("order-paid")</param>
public sealed record Email(string To, string Subject, string Html, string Text, string Kind);

/// <summary>Hands an e-mail over for delivery.</summary>
public interface IMailSender
{
    Task SendAsync(Email email, CancellationToken cancellationToken = default);
}

/// <summary>Sends over SMTP without authentication - to Mailpit on a developer's machine.</summary>
public sealed class SmtpMailSender : IMailSender
{
    private readonly MailOptions _options;
    private readonly ILogger<SmtpMailSender> _logger;

    public SmtpMailSender(IOptions<MailOptions> options, ILogger<SmtpMailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(Email email, CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage(MailAddressOf(_options.From), new MailAddress(email.To))
        {
            Subject = email.Subject,
            Body = email.Text
        };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(email.Html, null, "text/html"));

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort);
        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Sent the {Kind} e-mail", email.Kind);
    }

    private static MailAddress MailAddressOf(string from) => new(from);
}

/// <summary>
/// Writes what would be sent to the log, without the recipient's address or the body - the
/// delivery of a shop that sends no real e-mail.
/// </summary>
public sealed class LogMailSender : IMailSender
{
    private readonly ILogger<LogMailSender> _logger;

    public LogMailSender(ILogger<LogMailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(Email email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Would send the {Kind} e-mail \"{Subject}\" ({Length} characters of HTML)", email.Kind, email.Subject, email.Html.Length);
        return Task.CompletedTask;
    }
}
