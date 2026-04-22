using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Models.Enums;

/// <summary>How a full plan run ended (for dashboards and <see cref="AutomationPlanRunCompletedEvent" />).</summary>
public enum AutomationPlanRunOutcome
{
    /// <summary>All steps executed and the run returned a result.</summary>
    Completed,

    /// <summary>The run ended with <see cref="OperationCanceledException" /> (caller token, plan timeout, or cooperative cancel).</summary>
    Cancelled,

    /// <summary>The run ended with any other exception.</summary>
    Faulted
}