using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Models.Enums;

/// <summary>How a full plan run ended (used by dashboards and <see cref="AutomationPlanRunCompletedEvent" />).</summary>
public enum AutomationPlanRunOutcome
{
    /// <summary>Every step executed and the run returned a result.</summary>
    Completed,

    /// <summary>The run ended with <see cref="OperationCanceledException" /> (caller token, plan timeout, or a cooperative cancel).</summary>
    Cancelled,

    /// <summary>The run ended with some other exception.</summary>
    Faulted
}