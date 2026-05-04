namespace Lyo.Notification;

/// <summary>Publishes notifications to all registered <see cref="INotificationHandler{TNotification}" /> implementations.</summary>
public interface INotificationPublisher
{
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification;
}