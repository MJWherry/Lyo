using System.Diagnostics;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Plan;

/// <param name="RunId">Time-ordered run id for this invocation.</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AutomationPlanRunStartedEvent(Guid RunId, AutomationPlan Plan)
{
    /// <inheritdoc />
    public override string ToString()
        => $"AutomationPlanRunStartedEvent run={RunId:N} plan={Plan}";
}
