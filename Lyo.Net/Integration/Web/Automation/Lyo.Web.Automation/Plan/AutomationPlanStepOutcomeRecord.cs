using System.Diagnostics;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;

namespace Lyo.Web.Automation.Plan;

/// <summary>Unified step result for metrics (histograms, outcome counters).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct AutomationPlanStepOutcomeRecord(
    Guid RunId,
    Guid PlanStepId,
    Guid StepExecutionId,
    int StepIndex,
    AutomationPlanStepOutcome Outcome,
    TimeSpan Duration,
    AutomationStepDefinition Step,
    Exception? Error)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var planStep = PlanStepId == Guid.Empty ? "" : $" planStep={PlanStepId:N}";
        var err = Error is { } e ? AutomationDisplayText.Ellipsis(e.ToString()) : "";
        var errSuffix = err.Length > 0 ? $" error={err}" : "";
        return
            $"AutomationPlanStepOutcomeRecord run={RunId:N}{planStep} stepExec={StepExecutionId:N} index={StepIndex} outcome={Outcome} duration={Duration} step={Step}{errSuffix}";
    }
}