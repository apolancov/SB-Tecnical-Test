using Application.Requests.Notifications;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Requests.Notifications;

public sealed class DatabaseNotificationSender : INotificationSender
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseNotificationSender> _logger;

    public DatabaseNotificationSender(
        ApplicationDbContext context,
        ILogger<DatabaseNotificationSender> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendAsync(
        RequestNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (notification is null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        await _context.RequestNotifications.AddAsync(notification, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            notification.MarkSent();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Notification persisted. RequestId={RequestId} DestinationUserId={DestinationUserId} Channel={Channel} Subject={Subject}",
                notification.RequestId,
                notification.DestinationUserId,
                notification.Channel,
                notification.Subject);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Failed to persist notification. RequestId={RequestId} DestinationUserId={DestinationUserId}",
                notification.RequestId,
                notification.DestinationUserId);
            throw;
        }
    }
}