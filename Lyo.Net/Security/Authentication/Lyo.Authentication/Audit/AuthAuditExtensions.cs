using Lyo.Authentication.Models.Audit;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.Audit;

/// <summary>Helpers that emit <see cref="AuthAuditEvent" /> values so call sites do not have to build the record by hand.</summary>
public static class AuthAuditExtensions
{
    /// <summary>Builds and records an event. Never throws — exceptions from the recorder are caught and, best-effort, logged through <paramref name="logger" />.</summary>
    public static Task RecordAsync(
        this IAuthAuditRecorder recorder,
        IAuthAuditContextAccessor? context,
        ILogger? logger,
        AuthAuditEventKind kind,
        Guid? userId = null,
        string? subject = null,
        string? provider = null,
        string? outcome = null,
        string? reason = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var evt = new AuthAuditEvent(
            Guid.NewGuid(), DateTime.UtcNow, kind, userId, subject, provider, outcome, reason, context?.IpAddress, context?.UserAgent, context?.CorrelationId, metadata, tenantId);

        return SafeRecordAsync(recorder, evt, logger, ct);
    }

    private static async Task SafeRecordAsync(IAuthAuditRecorder recorder, AuthAuditEvent evt, ILogger? logger, CancellationToken ct)
    {
        try {
            await recorder.RecordAsync(evt, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            logger?.LogDebug(ex, "Auth audit record failed for kind {Kind}", evt.Kind);
        }
    }
}