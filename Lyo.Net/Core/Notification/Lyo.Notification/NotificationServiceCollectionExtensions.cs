using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Notification;

/// <summary>
/// Registers <see cref="INotificationPublisher" />. Register each <see cref="INotificationHandler{TNotification}" /> separately (e.g.
/// <c>AddSingleton&lt;INotificationHandler&lt;MyNotification&gt;, MyHandler&gt;()</c>).
/// </summary>
public static class NotificationServiceCollectionExtensions
{
    /// <summary>Registers <see cref="NotificationPublisher" /> as <see cref="INotificationPublisher" /> (singleton).</summary>
    public static IServiceCollection AddLyoNotification(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services, nameof(services));
        services.AddSingleton<INotificationPublisher, NotificationPublisher>();
        return services;
    }
}