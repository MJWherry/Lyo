using System.Diagnostics;

namespace Lyo.Diagnostic.Context;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiagnosticContextOptions
{
    public string? Environment { get; init; }

    public string? ServiceName { get; init; }

    public string? ServiceVersion { get; init; }

    /// <summary>Factory for generating occurrence IDs. Defaults to a new GUID string. Override to use your own correlation ID scheme.</summary>
    public Func<string> OccurrenceIdFactory { get; init; } = () => Guid.NewGuid().ToString("N");

    public static DiagnosticContextOptions Default { get; } = new();

    public override string ToString() => $"Service={ServiceName ?? "?"} v{ServiceVersion ?? "?"} Env={Environment ?? "?"}";
}