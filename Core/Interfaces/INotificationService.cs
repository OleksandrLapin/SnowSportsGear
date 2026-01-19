using Core.Models.Notifications;

namespace Core.Interfaces;

public interface INotificationService
{
    Task<bool> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);
    Task<int> SendBulkAsync(IEnumerable<NotificationRequest> requests, CancellationToken cancellationToken = default);
}
