using System.Diagnostics;

namespace Lyo.Diagnostic.Context;

/// <summary>Captures HTTP / gRPC request metadata at the point of exception. All properties are optional; populate what your host provides.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record RequestMetadata(
    string? CorrelationId,
    string? HttpMethod,
    string? Path,
    string? QueryString,
    string? UserIdentity,
    string? ClientIp,
    string? UserAgent,
    IReadOnlyDictionary<string, string> AdditionalProperties)
{
    /// <summary>Creates an empty metadata record for non-HTTP contexts.</summary>
    public static RequestMetadata Empty { get; } = new(null, null, null, null, null, null, null, new Dictionary<string, string>());

    public override string ToString()
        => HttpMethod is null && Path is null ? "(no request)" : $"{HttpMethod ?? "?"} {Path ?? "?"}";
}