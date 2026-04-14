namespace Lyo.FileStorage.Audit;

/// <summary>Append-only audit record for file storage operations. Never contains key material.</summary>
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
    Guid? CorrelationId = null);