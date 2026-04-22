using System.Diagnostics;
using Lyo.Web.Automation.Abstractions;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Plan;

/// <summary>
/// All element refs, list refs, and string variables for one point in time (typically the <see cref="AutomationPlanExecutionContext.Overall" /> result of a run).
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AutomationPlanBindings
{
    public AutomationPlanBindings(
        IReadOnlyDictionary<string, IWebAutomationElement> elements,
        IReadOnlyDictionary<string, IReadOnlyList<IWebAutomationElement>> elementLists,
        IReadOnlyDictionary<string, string> strings,
        IReadOnlyDictionary<string, IReadOnlyList<string>> stringLists)
    {
        Elements = elements;
        ElementLists = elementLists;
        Strings = strings;
        StringLists = stringLists;
    }

    public IReadOnlyDictionary<string, IWebAutomationElement> Elements { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<IWebAutomationElement>> ElementLists { get; }

    public IReadOnlyDictionary<string, string> Strings { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> StringLists { get; }

    public bool TryGetElement(string refName, out IWebAutomationElement? element)
    {
        if (Elements.TryGetValue(refName, out var found)) {
            element = found;
            return true;
        }

        element = null;
        return false;
    }

    public bool TryGetElementList(string refName, out IReadOnlyList<IWebAutomationElement>? list)
    {
        if (ElementLists.TryGetValue(refName, out var found)) {
            list = found;
            return true;
        }

        list = null;
        return false;
    }

    public bool TryGetString(string variableName, out string? value)
    {
        if (Strings.TryGetValue(variableName, out var found)) {
            value = found;
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetStringList(string variableName, out IReadOnlyList<string>? list)
    {
        if (StringLists.TryGetValue(variableName, out var found)) {
            list = found;
            return true;
        }

        list = null;
        return false;
    }

    /// <inheritdoc />
    public override string ToString()
        => $"AutomationPlanBindings elements={Elements.Count}, elementLists={ElementLists.Count}, strings={Strings.Count}, stringLists={StringLists.Count}";
}

/// <summary>
/// Immutable snapshot immediately after a single plan step finished.
/// <see cref="CompletedStepIndex" /> is zero-based: <c>0</c> = after <c>plan.Steps[0]</c>, etc.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AutomationPlanStepFrame
{
    public AutomationPlanStepFrame(int completedStepIndex, AutomationStepDefinition step, AutomationPlanBindings bindings)
    {
        CompletedStepIndex = completedStepIndex;
        Step = step;
        Bindings = bindings;
    }

    /// <summary>Bindings for this frame (same as <see cref="Bindings" /> properties on <see cref="AutomationPlanStepFrame" />).</summary>
    public AutomationPlanBindings Bindings { get; }

    /// <summary>Zero-based index of the step that just completed (matches <c>plan.Steps[CompletedStepIndex]</c>).</summary>
    public int CompletedStepIndex { get; }

    /// <summary>The step definition that was executed to produce this frame.</summary>
    public AutomationStepDefinition Step { get; }

    public IReadOnlyDictionary<string, IWebAutomationElement> Elements => Bindings.Elements;

    public IReadOnlyDictionary<string, IReadOnlyList<IWebAutomationElement>> ElementLists => Bindings.ElementLists;

    public IReadOnlyDictionary<string, string> Strings => Bindings.Strings;

    public IReadOnlyDictionary<string, IReadOnlyList<string>> StringLists => Bindings.StringLists;

    public bool TryGetElementList(string refName, out IReadOnlyList<IWebAutomationElement>? list)
        => Bindings.TryGetElementList(refName, out list);

    public bool TryGetElement(string refName, out IWebAutomationElement? element)
        => Bindings.TryGetElement(refName, out element);

    public bool TryGetStringList(string variableName, out IReadOnlyList<string>? list)
        => Bindings.TryGetStringList(variableName, out list);

    /// <inheritdoc />
    public override string ToString()
        => $"AutomationPlanStepFrame after step index {CompletedStepIndex}: {Step}";
}

/// <summary>
/// Result context for a completed plan: <see cref="Overall" /> is the cumulative final bindings; <see cref="Frames" /> is optional per-step history.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AutomationPlanExecutionContext
{
    internal AutomationPlanExecutionContext(AutomationPlanBindings overall, IReadOnlyList<AutomationPlanStepFrame> frames)
    {
        Overall = overall;
        Frames = frames;
    }

    /// <summary>
    /// Final merged bindings after the last step (element refs, string vars, lists). Prefer this for reading plan output.
    /// </summary>
    public AutomationPlanBindings Overall { get; }

    /// <summary>Historical snapshot after each step (for comparing state at a past step).</summary>
    public IReadOnlyList<AutomationPlanStepFrame> Frames { get; }

    /// <summary>Indexer into <see cref="Frames" /> — <c>context[0]</c> is state after the first plan step.</summary>
    public AutomationPlanStepFrame this[int completedStepIndex] => Frames[completedStepIndex];

    /// <summary>Number of steps executed (same as <see cref="Frames" />.Count).</summary>
    public int StepCount => Frames.Count;

    /// <summary>Snapshot after the step at <paramref name="completedStepIndex" /> (zero-based). Returns null if out of range.</summary>
    public AutomationPlanStepFrame? GetAfterCompletedStep(int completedStepIndex)
        => completedStepIndex >= 0 && completedStepIndex < Frames.Count ? Frames[completedStepIndex] : null;

    /// <summary>Same as <see cref="GetAfterCompletedStep" /> with 1-based step number (first step = 1 → frame index 0).</summary>
    public AutomationPlanStepFrame? GetAfterStepNumber(int oneBasedStepNumber)
    {
        if (oneBasedStepNumber < 1)
            return null;

        return GetAfterCompletedStep(oneBasedStepNumber - 1);
    }

    /// <summary>Try get an element list ref from a specific completed step (see <see cref="Frames" />).</summary>
    public bool TryGetElementListAtCompletedStep(int completedStepIndex, string refName, out IReadOnlyList<IWebAutomationElement>? list)
    {
        list = null;
        var frame = GetAfterCompletedStep(completedStepIndex);
        return frame != null && frame.TryGetElementList(refName, out list);
    }

    /// <summary>Try get a string list variable from a specific completed step.</summary>
    public bool TryGetStringListAtCompletedStep(int completedStepIndex, string variableName, out IReadOnlyList<string>? values)
    {
        values = null;
        var frame = GetAfterCompletedStep(completedStepIndex);
        return frame != null && frame.TryGetStringList(variableName, out values);
    }

    /// <inheritdoc />
    public override string ToString()
        => $"AutomationPlanExecutionContext steps={StepCount}";
}

/// <summary>Outcome of <see cref="AutomationPlanRunner.RunWithResultAsync" />: final extracted data plus execution context.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AutomationPlanRunResult
{
    public AutomationPlanRunResult(
        AutomationPlanExecutionSnapshot snapshot,
        AutomationPlanExecutionContext context)
    {
        Snapshot = snapshot;
        Context = context;
    }

    /// <summary>Final string / string-list variables (same data as <see cref="AutomationPlanExecutionContext.Overall" /> string tables).</summary>
    public AutomationPlanExecutionSnapshot Snapshot { get; }

    /// <summary>Bindings and optional per-step history.</summary>
    public AutomationPlanExecutionContext Context { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"AutomationPlanRunResult snapshot strings={Snapshot.Strings.Count}, lists={Snapshot.StringLists.Count}, context steps={Context.StepCount}";
}
