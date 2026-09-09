using System.Diagnostics;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;

namespace Lyo.Web.Automation.Plan;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AutomationPlanRunCompletedEvent(Guid RunId, AutomationPlan Plan, TimeSpan TotalDuration, AutomationPlanRunOutcome Outcome)
{
    /// <inheritdoc />
    public override string ToString() => $"AutomationPlanRunCompletedEvent run={RunId:N} outcome={Outcome} duration={TotalDuration} plan={Plan}";
}