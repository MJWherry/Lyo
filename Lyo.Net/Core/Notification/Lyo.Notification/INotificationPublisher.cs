namespace Lyo.Notification;

/// <summary>Publishes notifications to all registered <see cref="INotificationHandler{TNotification}" /> implementations.</summary>
public interface INotificationPublisher
{
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}