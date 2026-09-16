using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;
using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Playwright.Record;

/// <summary>One play notification. <see cref="Phase" /> is <c>start</c>, <c>outcome</c>, or <c>log</c>.</summary>
public sealed class AutomationStudioStepNotice
{
    /// <summary><c>start</c>, <c>outcome</c>, or <c>log</c>.</summary>
    public required string Phase { get; init; }

    /// <summary>Zero-based step index.</summary>
    public int StepIndex { get; init; }

    /// <summary>Step execution id assigned by the runner.</summary>
    public Guid StepExecutionId { get; init; }

    /// <summary>Set when <see cref="Phase" /> is <c>outcome</c>.</summary>
    public AutomationPlanStepOutcome? Outcome { get; init; }

    /// <summary>Failure message when the step did not succeed.</summary>
    public string? Error { get; init; }

    /// <summary>How long the step ran.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Local viewport PNG for a finished step, when the run directory captured one.</summary>
    public string? ScreenshotPath { get; init; }

    /// <summary>Strings, string lists, and context items after the step.</summary>
    public AutomationPlanContextSnapshot? Context { get; init; }

    /// <summary>One runner log line when <see cref="Phase" /> is <c>log</c>.</summary>
    public string? LogLine { get; init; }

    /// <summary>Step label when the step starts.</summary>
    public string? Label { get; init; }
}
