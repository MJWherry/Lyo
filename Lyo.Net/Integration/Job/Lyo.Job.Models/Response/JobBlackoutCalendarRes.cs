using System.Diagnostics;

namespace Lyo.Job.Models.Response;

/// <summary>A blackout calendar with its do-not-run windows.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobBlackoutCalendarRes(
    Guid Id,
    string Name,
    string? Description,
    bool Enabled,
    IReadOnlyList<JobBlackoutWindowRes>? BlackoutWindows)
{
    public override string ToString() => $"{Name}, {Description} (Enabled={Enabled}) Windows(C={BlackoutWindows?.Count ?? 0})";
}
