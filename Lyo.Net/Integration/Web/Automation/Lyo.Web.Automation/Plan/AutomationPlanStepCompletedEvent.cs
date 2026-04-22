using System.Diagnostics;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Plan;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AutomationPlanStepCompletedEvent(
    Guid RunId,
    Guid StepExecutionId,
    Guid PlanStepId,
    int StepIndex,
    AutomationStepDefinition Step,
    TimeSpan Duration)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var planStep = PlanStepId == Guid.Empty ? "" : $" planStep={PlanStepId:N}";
        return $"AutomationPlanStepCompletedEvent run={RunId:N} stepExec={StepExecutionId:N}{planStep} index={StepIndex} duration={Duration} {Step}";
    }
}
