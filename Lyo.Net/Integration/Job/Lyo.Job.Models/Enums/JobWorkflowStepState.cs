namespace Lyo.Job.Models.Enums;

/// <summary>Lifecycle state of an individual workflow step within a workflow run.</summary>
public enum JobWorkflowStepState
{
    Pending = 0,
    Running = 1,
    Finished = 2,
    Failed = 3,
    Skipped = 4
}