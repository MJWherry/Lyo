using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Plan;

/// <summary>Context for <see cref="AutomationPlanHooks.BeforeStepAsync"/> and <see cref="AutomationPlanHooks.AfterStepAsync"/>.</summary>
public class AutomationPlanStepContext
{
    public Guid RunId { get; init; }

    /// <summary>Unique id for this execution of the step (new each run).</summary>
    public Guid StepExecutionId { get; init; }

    /// <summary>Stable id from the step definition (assigned when the plan is built if omitted).</summary>
    public Guid PlanStepId { get; init; }

    public int StepIndex { get; init; }

    public AutomationPlan Plan { get; init; } = null!;

    public AutomationStepDefinition Step { get; init; } = null!;

    public IWebAutomationSession Session { get; init; } = null!;
}

/// <summary>Context when a step throws before being handled.</summary>
public sealed class AutomationPlanStepFailureContext : AutomationPlanStepContext
{
    public Exception Exception { get; init; } = null!;
}

/// <summary>Outcome of a single step after it finishes without throwing (passed to <see cref="AutomationPlanHooks.AfterStepAsync"/>).</summary>
public readonly record struct AutomationPlanStepResult(TimeSpan Duration);

/// <summary>Correlation fields for nested logging during a step (file I/O, downloads) when the ambient logger scope may not flow (e.g. <see cref="Task.Run"/>).</summary>
public readonly record struct AutomationPlanStepLogScope(
    Guid RunId,
    Guid StepExecutionId,
    Guid PlanStepId,
    int StepIndex,
    string StepLabel);

/// <summary>Optional callbacks around each step and on failure (screenshots, tracing, custom metrics).</summary>
public sealed class AutomationPlanHooks
{
    public Func<AutomationPlanStepContext, CancellationToken, ValueTask>? BeforeStepAsync { get; init; }

    public Func<AutomationPlanStepContext, AutomationPlanStepResult, CancellationToken, ValueTask>? AfterStepAsync { get; init; }

    public Func<AutomationPlanStepFailureContext, CancellationToken, ValueTask>? OnFailureAsync { get; init; }
}
