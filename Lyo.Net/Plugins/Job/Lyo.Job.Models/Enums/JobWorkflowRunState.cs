namespace Lyo.Job.Models.Enums;

/// <summary>Lifecycle state of one workflow run.</summary>
public enum JobWorkflowRunState
{
    Pending = 0,
    Running = 1,
    Finished = 2,
    Failed = 3,
    Cancelled = 4
}