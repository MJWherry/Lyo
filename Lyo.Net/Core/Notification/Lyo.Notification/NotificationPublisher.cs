using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Notification;

/// <inheritdoc />
public sealed class NotificationPublisher(IServiceProvider services, ILogger<NotificationPublisher> log) : INotificationPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentHelpers.ThrowIfNull(notification, nameof(notification));

        var handlers = services.GetServices<INotificationHandler<TNotification>>();
        foreach (var handler in handlers) {
            try {
                await handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) {
                log.LogError(ex, "Notification handler {HandlerType} failed for {NotificationType}", handler.GetType().FullName, typeof(TNotification).FullName);
            }
        }
    }
}