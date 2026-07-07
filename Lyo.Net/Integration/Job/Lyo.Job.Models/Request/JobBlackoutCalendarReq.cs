using System.Diagnostics;

namespace Lyo.Job.Models.Request;

/// <summary>Request to create or update a blackout calendar (do-not-run windows for schedules).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobBlackoutCalendarReq
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public List<JobBlackoutWindowReq> CreateBlackoutWindows { get; set; } = [];

    public override string ToString() => $"{Name}, {Description} (Enabled={Enabled}) Windows(C={CreateBlackoutWindows.Count})";
}
