using System.Diagnostics;

namespace Lyo.Web.Automation.Plan;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AutomationPlanStepFailedEvent(
    Guid RunId,
    Guid StepExecutionId,
    Guid PlanStepId,
    int StepIndex,
    AutomationStepDefinition Step,
    TimeSpan Duration,
    AutomationPlanStepOutcome Outcome,
    Exception Exception)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var planStep = PlanStepId == Guid.Empty ? "" : $" planStep={PlanStepId:N}";
        var ex = AutomationDisplayText.Ellipsis(Exception.ToString(), 160);
        return $"AutomationPlanStepFailedEvent run={RunId:N} stepExec={StepExecutionId:N}{planStep} index={StepIndex} outcome={Outcome} duration={Duration} step={Step} exception={ex}";
    }
}
