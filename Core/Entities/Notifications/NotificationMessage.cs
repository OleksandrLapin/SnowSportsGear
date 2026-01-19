namespace Core.Entities.Notifications;

public class NotificationMessage : BaseEntity
{
    public string TemplateKey { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public string? Error { get; set; }
    public string? Metadata { get; set; }
}
