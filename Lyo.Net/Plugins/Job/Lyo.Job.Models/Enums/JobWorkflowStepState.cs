namespace Lyo.Job.Models.Enums;

/// <summary>Lifecycle state of one workflow step inside a workflow run.</summary>
public enum JobWorkflowStepState
{
    Pending = 0,
    Running = 1,
    Finished = 2,
    Failed = 3,

    /// <summary>An earlier step stopped the workflow before this one could run.</summary>
    Skipped = 4,

    /// <summary>The step's condition did not match, so it was never dispatched. Distinct from <see cref="Skipped" />: nothing failed and the run is still on its happy path.</summary>
    ConditionNotMet = 5
}