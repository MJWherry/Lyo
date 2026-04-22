using System.Diagnostics;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Plan;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AutomationPlanStepStartingEvent(
    Guid RunId,
    Guid StepExecutionId,
    Guid PlanStepId,
    int StepIndex,
    AutomationStepDefinition Step)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var planStep = PlanStepId == Guid.Empty ? "" : $" planStep={PlanStepId:N}";
        return $"AutomationPlanStepStartingEvent run={RunId:N} stepExec={StepExecutionId:N}{planStep} index={StepIndex} {Step}";
    }
}
