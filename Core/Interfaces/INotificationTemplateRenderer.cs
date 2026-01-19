using Core.Entities.Notifications;
using Core.Models.Notifications;

namespace Core.Interfaces;

public interface INotificationTemplateRenderer
{
    RenderedNotification Render(NotificationTemplate template, IDictionary<string, string> tokens);
}
