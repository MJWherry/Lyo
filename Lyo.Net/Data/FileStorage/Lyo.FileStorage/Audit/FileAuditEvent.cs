using System.Diagnostics;

namespace Lyo.FileStorage.Audit;

/// <summary>Append-only audit row for file storage work. Never holds key material.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record FileAuditEvent(
    FileAuditEventType EventType,
    DateTime UtcTimestamp,
    Guid? FileId,
    string? TenantId,
    string? ActorId,
    string? DataEncryptionKeyId,
    string? DataEncryptionKeyVersion,
    FileAuditOutcome Outcome,
    string? Error = null,
    Guid? CorrelationId = null)
{
    /// <inheritdoc />
    public override string ToString()
        => $"FileAuditEvent: {EventType} {Outcome} FileId={FileId?.ToString() ?? "(none)"} @ {UtcTimestamp:u}";
}
