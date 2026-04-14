namespace Lyo.Notification;

/// <summary>Handles a single notification type. Multiple handlers per notification are supported.</summary>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}