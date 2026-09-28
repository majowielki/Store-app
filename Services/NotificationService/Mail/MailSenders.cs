using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net.Mime;

namespace Store.NotificationService.Mail;

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
        using var message = new MailMessage(new MailAddress(_options.From), new MailAddress(email.To))
        {
            Subject = email.Subject,
            Body = email.Text
        };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(email.Html, null, MediaTypeNames.Text.Html));

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort);
        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Sent the {Kind} e-mail", email.Kind);
    }
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
