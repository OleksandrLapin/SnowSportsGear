using System.Text.Json;
using Core.Entities.Notifications;
using Core.Interfaces;
using Core.Models.Notifications;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly StoreContext context;
    private readonly INotificationSender sender;
    private readonly INotificationTemplateRenderer renderer;
    private readonly ILogger<NotificationService> logger;

    public NotificationService(
        StoreContext context,
        INotificationSender sender,
        INotificationTemplateRenderer renderer,
        ILogger<NotificationService> logger)
    {
        this.context = context;
        this.sender = sender;
        this.renderer = renderer;
        this.logger = logger;
    }

    public async Task<bool> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var template = await context.NotificationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == request.TemplateKey, cancellationToken);

        if (template == null || !template.IsActive)
        {
            logger.LogWarning("Notification template {TemplateKey} not found or inactive", request.TemplateKey);
            return false;
        }

        var rendered = renderer.Render(template, request.Tokens);
        var message = new NotificationMessage
        {
            TemplateKey = template.Key,
            Channel = template.Channel,
            RecipientEmail = request.RecipientEmail,
            RecipientUserId = request.RecipientUserId,
            Subject = rendered.Subject,
            HtmlBody = rendered.HtmlBody,
            TextBody = rendered.TextBody,
            Status = NotificationStatus.Pending,
            Metadata = JsonSerializer.Serialize(request.Tokens)
        };

        context.NotificationMessages.Add(message);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await sender.SendAsync(message, cancellationToken);
            message.Status = NotificationStatus.Sent;
            message.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            message.Status = NotificationStatus.Failed;
            message.Error = ex.Message;
            logger.LogError(ex, "Failed to send notification {TemplateKey} to {Recipient}", template.Key, request.RecipientEmail);
        }

        await context.SaveChangesAsync(cancellationToken);

        return message.Status == NotificationStatus.Sent;
    }

    public async Task<int> SendBulkAsync(IEnumerable<NotificationRequest> requests, CancellationToken cancellationToken = default)
    {
        var sent = 0;
        foreach (var request in requests)
        {
            if (await SendAsync(request, cancellationToken))
            {
                sent++;
            }
        }
        return sent;
    }
}
