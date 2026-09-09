namespace Lyo.Web.Automation.Models.Enums;

/// <summary>Result of one automation step, used for metrics and tracing.</summary>
public enum AutomationPlanStepOutcome
{
    /// <summary>Step finished without throwing an exception.</summary>
    Success,

    /// <summary><see cref="OperationCanceledException" /> because the run or an outer token was cancelled.</summary>
    Cancelled,

    /// <summary><see cref="OperationCanceledException" /> because the per-step timeout fired (or the plan timeout).</summary>
    TimedOut,

    /// <summary>Some other exception from the step.</summary>
    Failed
}