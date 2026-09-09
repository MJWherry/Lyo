namespace Lyo.Notification;

/// <summary>Receives one notification type. Several handlers may subscribe to the same notification.</summary>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken ct = default);
}