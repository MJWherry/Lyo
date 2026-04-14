using System.Diagnostics;

namespace Lyo.Api.Models.Error;

/// <summary>One entry in the RFC 9457 <c>errors</c> array on <see cref="LyoProblemDetails" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ApiError(string Code, string Description, string? Stacktrace = null)
{
    public override string ToString() => $"{Code}: {Description}";
}
