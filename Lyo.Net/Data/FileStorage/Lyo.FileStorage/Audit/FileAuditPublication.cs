using Lyo.Metrics;
using Microsoft.Extensions.Logging;

namespace Lyo.FileStorage.Audit;

/// <summary>Raises file audit events to synchronous subscribers and DI-registered <see cref="IFileAuditEventHandler" /> instances with the same failure policy as storage.</summary>
public static class FileAuditPublication
{
    public static async Task PublishAsync(
        IReadOnlyList<IFileAuditEventHandler> handlers,
        EventHandler<FileAuditEventArgs>? fileAuditOccurred,
        object? sender,
        FileAuditEvent auditEvent,
        CancellationToken ct,
        ILogger logger,
        IMetrics metrics,
        string auditAppendFailedMetricName,
        bool throwOnAuditFailure)
    {
        if (fileAuditOccurred != null) {
            foreach (var d in fileAuditOccurred.GetInvocationList()) {
                try {
                    ((EventHandler<FileAuditEventArgs>)d).Invoke(sender, new(auditEvent, ct));
                }
                catch (Exception ex) {
                    metrics.IncrementCounter(auditAppendFailedMetricName);
                    logger.LogError(ex, "File audit subscriber failed for event {EventType}", auditEvent.EventType);
                    if (throwOnAuditFailure)
                        throw;
                }
            }
        }

        foreach (var h in handlers) {
            try {
                await h.HandleAsync(auditEvent, ct).ConfigureAwait(false);
            }
            catch (Exception ex) {
                metrics.IncrementCounter(auditAppendFailedMetricName);
                logger.LogError(ex, "Failed to handle file audit event {EventType}", auditEvent.EventType);
                if (throwOnAuditFailure)
                    throw;
            }
        }
    }
}