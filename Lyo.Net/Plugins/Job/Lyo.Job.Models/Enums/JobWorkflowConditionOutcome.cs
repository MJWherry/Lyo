namespace Lyo.Job.Models.Enums;

/// <summary>Verdict of a workflow step's condition.</summary>
public enum JobWorkflowConditionOutcome
{
    /// <summary>The condition matched, or the step has no condition at all.</summary>
    Run = 0,

    /// <summary>The condition did not match, or could not be evaluated. The step is marked <see cref="JobWorkflowStepState.ConditionNotMet" />.</summary>
    Skip = 1
}