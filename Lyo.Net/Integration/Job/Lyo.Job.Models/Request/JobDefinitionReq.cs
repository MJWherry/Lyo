using System.Diagnostics;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobDefinitionReq
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Type { get; set; }

    public string WorkerType { get; set; } = null!;

    public bool Enabled { get; set; } = true;

    public List<JobParameterReq> CreateParameters { get; set; } = [];

    public List<JobScheduleReq> CreateSchedules { get; set; } = [];

    public List<JobTriggerReq> CreateTriggers { get; set; } = [];

    public List<JobParallelRestrictionReq> CreateParallelRestrictions { get; set; } = [];

    public JobDefinitionReq() { }

    public JobDefinitionReq(string name, string? description = null, bool enabled = true)
    {
        Name = name;
        Description = description;
        Enabled = enabled;
    }

    public override string ToString()
        => $"{Name}, {Description} (Enabled={Enabled}) Params(C={CreateParameters.Count}) " + $"Schedules(C={CreateSchedules.Count}) " + $"Triggers(C={CreateTriggers.Count})";
}