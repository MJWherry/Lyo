namespace Lyo.Web.Automation.Models.Enums;

/// <summary>Result of a single automation step for metrics and tracing.</summary>
public enum AutomationPlanStepOutcome
{
    /// <summary>Step finished without throwing.</summary>
    Success,

    /// <summary><see cref="OperationCanceledException" /> because the run or outer token was cancelled.</summary>
    Cancelled,

    /// <summary><see cref="OperationCanceledException" /> because the per-step timeout fired (or plan timeout).</summary>
    TimedOut,

    /// <summary>Any other exception from the step.</summary>
    Failed
}