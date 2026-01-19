using System.Net;
using System.Net.Mail;
using Core.Entities.Notifications;
using Core.Interfaces;
using Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class SmtpEmailSender : INotificationSender
{
    private readonly EmailSettings settings;
    private readonly ILogger<SmtpEmailSender> logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        this.settings = settings.Value;
        this.logger = logger;
    }

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.FromEmail))
        {
            var error = "Email settings not configured.";
            logger.LogWarning("{Error} Unable to send to {Recipient}", error, message.RecipientEmail);
            throw new InvalidOperationException(error);
        }

        using var mail = new MailMessage
        {
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true,
            From = new MailAddress(settings.FromEmail, settings.FromName)
        };

        mail.To.Add(message.RecipientEmail);

        if (!string.IsNullOrWhiteSpace(message.TextBody))
        {
            var textView = AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain");
            mail.AlternateViews.Add(textView);
        }

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(settings.UserName))
        {
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        }

        await client.SendMailAsync(mail, cancellationToken);
    }
}
