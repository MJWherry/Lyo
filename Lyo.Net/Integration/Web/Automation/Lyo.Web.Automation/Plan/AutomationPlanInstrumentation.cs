namespace Lyo.Web.Automation.Plan;

/// <summary>How a full plan run ended (for dashboards and <see cref="AutomationPlanRunCompletedEvent"/>).</summary>
public enum AutomationPlanRunOutcome
{
    /// <summary>All steps executed and the run returned a result.</summary>
    Completed,

    /// <summary>The run ended with <see cref="OperationCanceledException"/> (caller token, plan timeout, or cooperative cancel).</summary>
    Cancelled,

    /// <summary>The run ended with any other exception.</summary>
    Faulted
}

/// <summary>Result of a single automation step for metrics and tracing.</summary>
public enum AutomationPlanStepOutcome
{
    /// <summary>Step finished without throwing.</summary>
    Success,

    /// <summary><see cref="OperationCanceledException" /> because the run or outer token was cancelled.</summary>
    Cancelled,

    /// <summary><see cref="OperationCanceledException" /> because the per-step timeout fired (or plan timeout).</summary>
    TimedOut,

    /// <summary>Any other exception from the step.</summary>
    Failed
}

/// <summary>Optional metrics / tracing sink for automation plan runs (implement to push to OpenTelemetry, etc.).</summary>
public interface IAutomationPlanInstrumentation
{
    void OnRunStarted(in AutomationPlanRunStartedEvent e);

    void OnStepStarting(in AutomationPlanStepStartingEvent e);

    void OnStepCompleted(in AutomationPlanStepCompletedEvent e);

    void OnStepFailed(in AutomationPlanStepFailedEvent e);

    void OnRunCompleted(in AutomationPlanRunCompletedEvent e);

    /// <summary>
    /// Called exactly once after each step finishes (success or failure). Use for counters, histograms of <see cref="AutomationPlanStepOutcomeRecord.Duration"/>, and outcome labels.
    /// </summary>
    void OnStepOutcome(in AutomationPlanStepOutcomeRecord e);
}

/// <summary>Optional base class: override only the hooks you need (all default to no-op).</summary>
public abstract class AutomationPlanInstrumentationBase : IAutomationPlanInstrumentation
{
    public virtual void OnRunStarted(in AutomationPlanRunStartedEvent e) { }

    public virtual void OnStepStarting(in AutomationPlanStepStartingEvent e) { }

    public virtual void OnStepCompleted(in AutomationPlanStepCompletedEvent e) { }

    public virtual void OnStepFailed(in AutomationPlanStepFailedEvent e) { }

    public virtual void OnRunCompleted(in AutomationPlanRunCompletedEvent e) { }

    public virtual void OnStepOutcome(in AutomationPlanStepOutcomeRecord e) { }
}
