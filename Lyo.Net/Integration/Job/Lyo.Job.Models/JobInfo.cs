using System.Diagnostics;
using Lyo.Job.Models.Response;

namespace Lyo.Job.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobInfo(
    JobDefinitionRes Definition,
    JobScheduleRes? Schedule = null,
    JobTriggerRes? Trigger = null,
    JobRunRes? LastRun = null,
    JobRunRes? LastSuccessfulRun = null,
    JobRunRes? LastFailedRun = null)
{
    public override string ToString() => $"{Definition} | Last Run {LastRun} | Last Success {LastSuccessfulRun} | Last Failed {LastFailedRun}";
}