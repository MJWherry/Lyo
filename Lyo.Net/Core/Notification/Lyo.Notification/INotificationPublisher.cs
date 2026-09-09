namespace Lyo.Notification;

/// <summary>Sends a notification to every registered <see cref="INotificationHandler{TNotification}" />.</summary>
public interface INotificationPublisher
{
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification;
}