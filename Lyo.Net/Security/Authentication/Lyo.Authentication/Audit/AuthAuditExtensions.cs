using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Models.Audit;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.Audit;

/// <summary>Convenience helpers for emitting <see cref="AuthAuditEvent"/> values without forcing every call site to construct the record manually.</summary>
public static class AuthAuditExtensions
{
    /// <summary>
    /// Builds and records an event. Always non-throwing — any exception from the underlying recorder is caught and (best-effort) logged through <paramref name="logger"/>.
    /// </summary>
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
            Id: Guid.NewGuid(),
            Timestamp: DateTime.UtcNow,
            Kind: kind,
            UserId: userId,
            Subject: subject,
            Provider: provider,
            Outcome: outcome,
            Reason: reason,
            IpAddress: context?.IpAddress,
            UserAgent: context?.UserAgent,
            CorrelationId: context?.CorrelationId,
            Metadata: metadata,
            TenantId: tenantId);

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
