using System.Net;
using System.Net.Mail;
using System.IO;
using System.Text;
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
        if (string.IsNullOrWhiteSpace(settings.FromEmail))
        {
            var error = "Email settings not configured.";
            logger.LogWarning("{Error} Unable to send to {Recipient}", error, message.RecipientEmail);
            throw new InvalidOperationException(error);
        }

        var pickupDirectory = ResolvePickupDirectory(settings.PickupDirectory);
        var usePickupDirectory = string.IsNullOrWhiteSpace(settings.Host) && !string.IsNullOrWhiteSpace(pickupDirectory);

        if (string.IsNullOrWhiteSpace(settings.Host) && !usePickupDirectory)
        {
            var error = "Email settings not configured.";
            logger.LogWarning("{Error} Unable to send to {Recipient}", error, message.RecipientEmail);
            throw new InvalidOperationException(error);
        }

        var htmlBody = string.IsNullOrWhiteSpace(message.HtmlBody)
            ? message.TextBody ?? string.Empty
            : message.HtmlBody;

        using var mail = new MailMessage
        {
            Subject = message.Subject,
            Body = htmlBody,
            IsBodyHtml = !string.IsNullOrWhiteSpace(message.HtmlBody),
            From = new MailAddress(settings.FromEmail, settings.FromName)
        };
        mail.BodyEncoding = Encoding.UTF8;
        mail.SubjectEncoding = Encoding.UTF8;

        mail.To.Add(message.RecipientEmail);

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            var htmlView = AlternateView.CreateAlternateViewFromString(message.HtmlBody, Encoding.UTF8, "text/html");
            mail.AlternateViews.Add(htmlView);
        }

        using var client = new SmtpClient(string.IsNullOrWhiteSpace(settings.Host) ? "localhost" : settings.Host, settings.Port);

        if (usePickupDirectory)
        {
            Directory.CreateDirectory(pickupDirectory);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = pickupDirectory;
        }
        else
        {
            client.EnableSsl = settings.UseSsl;
            if (!string.IsNullOrWhiteSpace(settings.UserName))
            {
                client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
            }
        }

        await client.SendMailAsync(mail, cancellationToken);

        if (usePickupDirectory)
        {
            logger.LogInformation("Email saved to pickup directory {PickupDirectory} for {Recipient}", pickupDirectory, message.RecipientEmail);
        }
    }

    private string ResolvePickupDirectory(string pickupDirectory)
    {
        if (string.IsNullOrWhiteSpace(pickupDirectory)) return string.Empty;
        return Path.IsPathRooted(pickupDirectory)
            ? pickupDirectory
            : Path.Combine(Directory.GetCurrentDirectory(), pickupDirectory);
    }
}
