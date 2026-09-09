namespace Lyo.Job.Models.Enums;

/// <summary>Lifecycle state of one workflow step inside a workflow run.</summary>
public enum JobWorkflowStepState
{
    Pending = 0,
    Running = 1,
    Finished = 2,
    Failed = 3,
    Skipped = 4
}