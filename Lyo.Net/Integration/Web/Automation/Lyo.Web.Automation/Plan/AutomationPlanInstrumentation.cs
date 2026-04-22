namespace Lyo.Web.Automation.Plan;

/// <summary>Optional metrics / tracing sink for automation plan runs (implement to push to OpenTelemetry, etc.).</summary>
public interface IAutomationPlanInstrumentation
{
    void OnRunStarted(in AutomationPlanRunStartedEvent e);

    void OnStepStarting(in AutomationPlanStepStartingEvent e);

    void OnStepCompleted(in AutomationPlanStepCompletedEvent e);

    void OnStepFailed(in AutomationPlanStepFailedEvent e);

    void OnRunCompleted(in AutomationPlanRunCompletedEvent e);

    /// <summary>
    /// Called exactly once after each step finishes (success or failure). Use for counters, histograms of <see cref="AutomationPlanStepOutcomeRecord.Duration" />, and outcome
    /// labels.
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