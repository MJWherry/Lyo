using System.Diagnostics;

namespace Lyo.Diagnostic.Inbox;

/// <summary>Rolled-up view of occurrences for one <see cref="ErrorGroupKey" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ErrorGroupSummary(ErrorGroupKey Key, int OccurrenceCount, DateTimeOffset FirstSeen, DateTimeOffset LastSeen)
{
    public override string ToString() => $"{Key} count={OccurrenceCount}";
}
