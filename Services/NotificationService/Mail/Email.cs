namespace Store.NotificationService.Mail;

/// <summary>An e-mail ready to send.</summary>
/// <param name="To">The recipient's address</param>
/// <param name="Subject">The subject line</param>
/// <param name="Html">The HTML body</param>
/// <param name="Text">The plain-text body, for clients that show no HTML</param>
/// <param name="Kind">What it is about, for the logs (the template's <c>Kind</c>)</param>
public sealed record Email(string To, string Subject, string Html, string Text, string Kind);

/// <summary>The e-mail an event becomes: one template per kind of event.</summary>
/// <typeparam name="TEvent">The event the e-mail is written from</typeparam>
public interface IMailTemplate<in TEvent> where TEvent : class
{
    Email Render(TEvent message);
}

/// <summary>Hands an e-mail over for delivery.</summary>
public interface IMailSender
{
    Task SendAsync(Email email, CancellationToken cancellationToken = default);
}
