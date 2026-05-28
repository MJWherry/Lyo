using Lyo.Authentication.Models.Audit;

namespace Lyo.Authentication.Postgres.Database;

/// <summary>EF entity for the <c>[user].[event]</c> audit table. Persistence of <see cref="AuthAuditEvent" />.</summary>
public sealed class UserEventEntity
{
    /// <summary>Server-generated row id (Guid v4). PK.</summary>
    public Guid Id { get; set; }

    /// <summary>UTC at the moment the event happened.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Stored as the enum's string name (e.g. <c>JwtIssued</c>) so logs/queries are human-readable. Width is bounded to 64 chars.</summary>
    public AuthAuditEventKind Kind { get; set; }

    /// <summary>Optional Lyo user the event is about. Soft FK to <c>[user].[user].id</c>.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Optional tenant scope. <see langword="null" /> means system / no tenant; non-null indicates a tenant-scoped event.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Free-form subject id (token id, handoff id, external sub, …).</summary>
    public string? Subject { get; set; }

    /// <summary>Provider name (<c>google</c>, <c>local</c>, …).</summary>
    public string? Provider { get; set; }

    /// <summary>"success"/"failure". Lowercase.</summary>
    public string? Outcome { get; set; }

    /// <summary>Closed-vocabulary reason code (e.g. <c>OidcStateInvalid</c>).</summary>
    public string? Reason { get; set; }

    /// <summary>Caller IP at the moment of the event.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Caller User-Agent header.</summary>
    public string? UserAgent { get; set; }

    /// <summary>Correlation/trace id.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Open-ended metadata, JSON-serialized.</summary>
    public string? MetadataJson { get; set; }
}