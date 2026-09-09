using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Notification;

/// <summary>
/// Wires <see cref="INotificationPublisher" />. Each <see cref="INotificationHandler{TNotification}" /> is registered on its own (for example
/// <c>AddSingleton&lt;INotificationHandler&lt;MyNotification&gt;, MyHandler&gt;()</c>).
/// </summary>
public static class NotificationServiceCollectionExtensions
{
    /// <summary>Adds <see cref="NotificationPublisher" /> as the singleton <see cref="INotificationPublisher" />.</summary>
    public static IServiceCollection AddLyoNotification(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.AddSingleton<INotificationPublisher, NotificationPublisher>();
        return services;
    }
}