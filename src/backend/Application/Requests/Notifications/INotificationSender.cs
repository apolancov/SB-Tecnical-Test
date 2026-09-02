using Domain.Entities;

namespace Application.Requests.Notifications;

public interface INotificationSender
{
    Task SendAsync(
        RequestNotification notification,
        CancellationToken cancellationToken = default);
}