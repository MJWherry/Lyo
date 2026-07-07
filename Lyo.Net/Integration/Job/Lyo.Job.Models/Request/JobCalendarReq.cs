using System.Diagnostics;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobCalendarReq
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public List<JobCalendarWindowReq> CreateWindows { get; set; } = [];

    public override string ToString() => $"{Name}, {Description} (Enabled={Enabled}) Windows(C={CreateWindows.Count})";
}
