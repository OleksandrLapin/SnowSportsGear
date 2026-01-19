using System.Collections.Generic;

namespace Core.Models.Notifications;

public record NotificationRequest(
    string TemplateKey,
    string RecipientEmail,
    IDictionary<string, string> Tokens,
    string? RecipientUserId = null
);
