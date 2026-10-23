using System.Diagnostics;

namespace Lyo.Authentication.Models.Response;

/// <summary>User-scope row returned by Get / QueryConcrete.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AuthScopeRes(Guid Id, Guid UserId, string Name, DateTime CreatedTimestamp, DateTime? UpdatedTimestamp);