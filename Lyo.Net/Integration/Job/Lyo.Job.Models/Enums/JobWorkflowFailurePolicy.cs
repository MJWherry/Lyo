namespace Lyo.Job.Models.Enums;

/// <summary>How a workflow proceeds when a step fails.</summary>
public enum JobWorkflowFailurePolicy
{
    /// <summary>Stop the workflow and mark remaining steps as skipped.</summary>
    Stop = 0,

    /// <summary>Continue executing steps whose dependencies are satisfied.</summary>
    Continue = 1
}