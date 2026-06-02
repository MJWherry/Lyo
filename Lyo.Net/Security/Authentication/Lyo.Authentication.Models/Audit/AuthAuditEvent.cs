using System.Diagnostics;

namespace Lyo.Authentication.Models.Audit;

/// <summary>
/// One auth-subsystem audit row. Designed to map 1:1 to the <c>[user].[event]</c> table on the Postgres side, and to a "fire and forget" event sink (logger, queue, …)
/// otherwise. Always carries the <see cref="Kind" /> (closed taxonomy), the timestamp (UTC), and an optional <see cref="UserId" /> when the event is user-attached; everything else is
/// loosely typed metadata to keep the persistence shape stable across schema evolutions.
/// </summary>
/// <param name="Id">Server-generated row id (GUID v4). Set by the caller — recorders treat this as the primary key.</param>
/// <param name="Timestamp">UTC at the moment the event happened (not the moment it was persisted).</param>
/// <param name="Kind">The audit-event taxonomy member.</param>
/// <param name="UserId">The Lyo user the event is about, when one exists.</param>
/// <param name="Subject">Free-form subject identifier (e.g. the token id, the handoff code id, the external sub). Optional.</param>
/// <param name="Provider">External identity provider name (<c>google</c>, <c>keycloak:lyo</c>, <c>local</c>), when relevant.</param>
/// <param name="Outcome"><c>success</c> or <c>failure</c>. Lowercase, ASCII. Optional.</param>
/// <param name="Reason">Stable, closed-vocabulary reason code (e.g. <c>OidcStateInvalid</c>, <c>EmailNotVerified</c>, <c>rotated</c>). Suitable for metric labels.</param>
/// <param name="IpAddress">Caller IP at the moment of the event, when one exists.</param>
/// <param name="UserAgent">Caller User-Agent at the moment of the event, when one exists.</param>
/// <param name="CorrelationId">Trace/request id (W3C traceparent or otherwise) that the event participates in.</param>
/// <param name="Metadata">Open-ended extra context. Recorders typically persist as <c>jsonb</c>.</param>
/// <param name="TenantId">Optional tenant scope. <c>null</c> means system / no tenant; non-null indicates a tenant-scoped event.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AuthAuditEvent(
    Guid Id,
    DateTime Timestamp,
    AuthAuditEventKind Kind,
    Guid? UserId = null,
    string? Subject = null,
    string? Provider = null,
    string? Outcome = null,
    string? Reason = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? CorrelationId = null,
    IReadOnlyDictionary<string, object?>? Metadata = null,
    Guid? TenantId = null)
{
    public override string ToString()
        => $"AuthAuditEvent: kind={Kind}, user={UserId}, outcome={Outcome ?? "?"}";
}