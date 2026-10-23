using System.Diagnostics;

namespace Lyo.Authentication.Models.Response;

/// <summary>User-claim row returned by Get / QueryConcrete.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AuthClaimRes(Guid Id, Guid UserId, string Type, string Value, DateTime CreatedTimestamp, DateTime? UpdatedTimestamp);