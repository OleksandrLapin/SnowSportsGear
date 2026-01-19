namespace Core.Entities.Notifications;

public class NotificationTemplate : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public NotificationCategory Category { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
    public string Subject { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? CtaLabel { get; set; }
    public string? CtaUrl { get; set; }
    public string? Footer { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
