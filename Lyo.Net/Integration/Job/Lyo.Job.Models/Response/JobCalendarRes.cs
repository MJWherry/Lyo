using System.Diagnostics;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobCalendarRes(
    Guid Id,
    string Name,
    string? Description,
    bool Enabled,
    IReadOnlyList<JobCalendarWindowRes>? Windows)
{
    public override string ToString() => $"{Name}, {Description} (Enabled={Enabled})";
}
