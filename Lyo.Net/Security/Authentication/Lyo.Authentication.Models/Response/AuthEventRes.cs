using System.Diagnostics;
using Lyo.Authentication.Models.Audit;

namespace Lyo.Authentication.Models.Response;

/// <summary>Auth audit event for admin QueryConcrete / Get.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AuthEventRes(
    Guid Id,
    DateTime Timestamp,
    AuthAuditEventKind Kind,
    Guid? UserId,
    Guid? TenantId,
    string? Subject,
    string? Provider,
    string? Outcome,
    string? Reason,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId,
    string? MetadataJson);