using System.Diagnostics;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Plan;

/// <summary>Raised when an automation plan run begins.</summary>
/// <param name="RunId">Time-ordered run id for this invocation.</param>
/// <param name="Plan">The automation plan being executed.</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AutomationPlanRunStartedEvent(Guid RunId, AutomationPlan Plan)
{
    /// <inheritdoc />
    public override string ToString() => $"AutomationPlanRunStartedEvent run={RunId:N} plan={Plan}";
}