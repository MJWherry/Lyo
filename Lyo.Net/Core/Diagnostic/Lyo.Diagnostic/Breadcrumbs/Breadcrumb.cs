using System.Diagnostics;

namespace Lyo.Diagnostic.Breadcrumbs;

/// <summary>One timestamped step in a breadcrumb trail, usually recorded before a failure for triage context.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Breadcrumb(DateTimeOffset At, string Category, string Message, IReadOnlyDictionary<string, string>? Data = null)
{
    public override string ToString() => $"[{Category}] {Message}";
}