using Core.Entities.Notifications;

namespace Core.Interfaces;

public interface INotificationSender
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
