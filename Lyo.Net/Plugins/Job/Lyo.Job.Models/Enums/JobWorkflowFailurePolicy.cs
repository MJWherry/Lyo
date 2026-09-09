namespace Lyo.Job.Models.Enums;

/// <summary>How a workflow continues when a step fails.</summary>
public enum JobWorkflowFailurePolicy
{
    /// <summary>Stop the workflow and mark leftover steps as skipped.</summary>
    Stop = 0,

    /// <summary>Keep running steps whose dependencies are satisfied.</summary>
    Continue = 1
}