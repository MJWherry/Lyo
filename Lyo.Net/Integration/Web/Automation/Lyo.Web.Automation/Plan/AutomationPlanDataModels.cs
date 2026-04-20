using System.Diagnostics;
using Lyo.Formatter;

namespace Lyo.Web.Automation.Plan;

/// <summary>Runtime options for steps that use HTTP or the file system (not serialized as part of <see cref="Lyo.Web.Automation.Models.AutomationPlan" />).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AutomationPlanRuntimeOptions
{
    /// <summary>When set, <see cref="Lyo.Web.Automation.Models.DownloadUrlsToDirectoryAutomationStep" /> can fetch each URL and write under the target directory.</summary>
    public HttpClient? HttpClient { get; init; }

    /// <summary>Optional prefix for generated file names during download (default <c>download</c>).</summary>
    public string DownloadFileNamePrefix { get; init; } = "download";

    /// <summary>Maximum duration for the entire plan (combined with the run <see cref="CancellationToken" />).</summary>
    public TimeSpan? PlanTimeout { get; init; }

    /// <summary>Default maximum duration for each step when the step does not set <see cref="Lyo.Web.Automation.Models.AutomationStepDefinition.StepTimeout"/>.</summary>
    public TimeSpan? DefaultStepTimeout { get; init; }

    /// <summary>Optional callbacks around each step.</summary>
    public AutomationPlanHooks? Hooks { get; init; }

    /// <summary>Optional metrics / tracing sink.</summary>
    public IAutomationPlanInstrumentation? Instrumentation { get; init; }

    /// <summary>
    /// When set, step templates are validated with <see cref="IFormatterService" /> (SmartFormat) before expansion.
    /// Placeholders use single braces, e.g. <c>{page.url}</c>, or legacy double braces <c>{{page.url}}</c> which are normalized first.
    /// </summary>
    public IFormatterService? Formatter { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"AutomationPlanRuntimeOptions HttpClient={(HttpClient != null ? "set" : "null")}, downloadPrefix={DownloadFileNamePrefix}, planTimeout={PlanTimeout}, defaultStepTimeout={DefaultStepTimeout}, hooks={(Hooks != null ? "set" : "null")}, instrumentation={(Instrumentation != null ? "set" : "null")}, formatter={(Formatter != null ? "set" : "null")}";
}

/// <summary>String and list variables captured after a plan run (element refs are not included).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AutomationPlanExecutionSnapshot
{
    public AutomationPlanExecutionSnapshot(
        IReadOnlyDictionary<string, string> strings,
        IReadOnlyDictionary<string, IReadOnlyList<string>> stringLists)
    {
        Strings = strings;
        StringLists = stringLists;
    }

    public IReadOnlyDictionary<string, string> Strings { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> StringLists { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"AutomationPlanExecutionSnapshot strings={Strings.Count}, stringLists={StringLists.Count}";
}
