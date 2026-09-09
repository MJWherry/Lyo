using System.Diagnostics;

namespace Lyo.Authentication.Models.Audit;

/// <summary>
/// One auth-subsystem audit row. Maps 1:1 to <c>[user].[event]</c> on Postgres, or to a fire-and-forget sink (logger, queue, …)
/// otherwise. Always carries <see cref="Kind" /> (closed taxonomy), a UTC timestamp, and optional <see cref="UserId" /> when the event is user-attached; everything else is
/// loosely typed metadata so the persistence shape stays stable as the schema evolves.
/// </summary>
/// <param name="Id">Server-generated row id (GUID v4). Caller sets this — recorders treat it as the primary key.</param>
/// <param name="Timestamp">UTC when the event happened (not when it was persisted).</param>
/// <param name="Kind">Audit-event taxonomy member.</param>
/// <param name="UserId">Lyo user the event is about, when one exists.</param>
/// <param name="Subject">Free-form subject id (token id, handoff code id, external sub). Optional.</param>
/// <param name="Provider">External identity provider name (<c>google</c>, <c>keycloak:lyo</c>, <c>local</c>), when relevant.</param>
/// <param name="Outcome"><c>success</c> or <c>failure</c>. Lowercase ASCII. Optional.</param>
/// <param name="Reason">Stable, closed-vocabulary reason code (e.g. <c>OidcStateInvalid</c>, <c>EmailNotVerified</c>, <c>rotated</c>). Fine for metric labels.</param>
/// <param name="IpAddress">Caller IP at event time, when known.</param>
/// <param name="UserAgent">Caller User-Agent at event time, when known.</param>
/// <param name="CorrelationId">Trace/request id (W3C traceparent or otherwise) this event participates in.</param>
/// <param name="Metadata">Open extra context. Recorders typically persist as <c>jsonb</c>.</param>
/// <param name="TenantId">Optional tenant scope. <c>null</c> means system / no tenant; non-null is tenant-scoped.</param>
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
    public override string ToString() => $"AuthAuditEvent: kind={Kind}, user={UserId}, outcome={Outcome ?? "?"}";
}