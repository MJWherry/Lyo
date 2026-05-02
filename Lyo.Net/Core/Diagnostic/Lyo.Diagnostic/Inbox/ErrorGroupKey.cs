using System.Diagnostics;

namespace Lyo.Diagnostic.Inbox;

/// <summary>Stable grouping key for error inbox rollups (fingerprint + semantic kind + service).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct ErrorGroupKey(string Fingerprint, string ExceptionKind, string? ServiceName)
{
    public override string ToString() => $"{Fingerprint}:{ExceptionKind}:{ServiceName ?? ""}";
}
