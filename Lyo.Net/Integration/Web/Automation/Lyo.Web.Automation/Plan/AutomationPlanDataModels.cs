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

    /// <summary>Default maximum duration for each step when the step does not set <see cref="Lyo.Web.Automation.Models.AutomationStepDefinition.StepTimeout" />.</summary>
    public TimeSpan? DefaultStepTimeout { get; init; }

    /// <summary>Optional callbacks around each step.</summary>
    public AutomationPlanHooks? Hooks { get; init; }

    /// <summary>Optional metrics / tracing sink.</summary>
    public IAutomationPlanInstrumentation? Instrumentation { get; init; }

    /// <summary>
    /// When set, each run creates a directory (see <see cref="AutomationPlanRunDirectoryOptions" />) with optional <c>logs/</c> transcript, <c>snapshots/</c> PNGs, and
    /// <c>variables/</c> JSON. When <see langword="null" />, no run-scoped files are written.
    /// </summary>
    public AutomationPlanRunDirectoryOptions? PlanRunDirectory { get; init; }

    /// <summary>
    /// When set, step templates are validated with <see cref="IFormatterService" /> (SmartFormat) before expansion. Placeholders use single braces, e.g. <c>{page.url}</c>, or
    /// legacy double braces <c>{{page.url}}</c> which are normalized first.
    /// </summary>
    public IFormatterService? Formatter { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"HttpClient={(HttpClient != null ? "set" : "null")}, downloadPrefix={DownloadFileNamePrefix}, planTimeout={PlanTimeout}, defaultStepTimeout={DefaultStepTimeout}, hooks={(Hooks != null ? "set" : "null")}, instrumentation={(Instrumentation != null ? "set" : "null")}, planRunDirectory={(PlanRunDirectory != null ? "set" : "null")}, formatter={(Formatter != null ? "set" : "null")}";
}

/// <summary>
/// Disk layout for a single plan run (not serialized). Default layout: <c>{<see cref="RootDirectory" />}/{run id}/logs/run.log</c>, <c>…/snapshots/*.png</c>,
/// <c>…/variables/*.json</c> when nesting is enabled.
/// </summary>
public sealed class AutomationPlanRunDirectoryOptions
{
    /// <summary>Parent folder for runs. When <see cref="NestRunUnderRoot" /> is true, each run uses a subfolder here.</summary>
    public required string RootDirectory { get; init; }

    /// <summary>When true (default), artifacts go under <c>Path.Combine(<see cref="RootDirectory" />, folder)</c> where folder is <see cref="RunFolderName" /> or the run id.</summary>
    public bool NestRunUnderRoot { get; init; } = true;

    /// <summary>When <see cref="NestRunUnderRoot" /> is true, names the run subfolder; default is the run <c>Guid</c> (<c>N</c> format).</summary>
    public string? RunFolderName { get; init; }

    /// <summary>Subdirectory under the run root for the text transcript.</summary>
    public string LogsSubdirectory { get; init; } = "logs";

    /// <summary>Subdirectory under the run root for viewport PNGs.</summary>
    public string SnapshotsSubdirectory { get; init; } = "snapshots";

    /// <summary>Subdirectory under the run root for variable JSON dumps.</summary>
    public string VariablesSubdirectory { get; init; } = "variables";

    /// <summary>UTF-8 line log of run and step events (under <see cref="LogsSubdirectory" />).</summary>
    public bool WriteRunLogFile { get; init; } = true;

    /// <summary>File name for <see cref="WriteRunLogFile" />.</summary>
    public string RunLogFileName { get; init; } = "run.log";

    /// <summary>Viewport PNGs via <see cref="Lyo.Web.Automation.Abstractions.IWebAutomationPage.TakeViewportSnapshotPngAsync" />.</summary>
    public bool WriteSnapshots { get; init; } = true;

    /// <summary>Capture before each step body (after <see cref="AutomationPlanHooks.BeforeStepAsync" />).</summary>
    public bool SnapshotBeforeEachStep { get; init; }

    /// <summary>Capture after each successful step.</summary>
    public bool SnapshotAfterEachSuccessfulStep { get; init; } = true;

    /// <summary>Capture when a step throws.</summary>
    public bool SnapshotOnStepFailure { get; init; } = true;

    /// <summary>JSON files for string / string-list variables (not element refs).</summary>
    public bool WriteVariables { get; init; } = true;

    /// <summary>Write <c>step_{index:000}_after.json</c> after each successful step.</summary>
    public bool VariablesAfterEachSuccessfulStep { get; init; } = true;

    /// <summary>Write <c>step_{index:000}_failed.json</c> when a step throws.</summary>
    public bool VariablesOnStepFailure { get; init; } = true;

    /// <summary>Write <see cref="FinalVariablesFileName" /> when the run finishes or faults (best-effort).</summary>
    public bool VariablesOnRunEnd { get; init; } = true;

    /// <summary>File name under <see cref="VariablesSubdirectory" /> for the last variable snapshot.</summary>
    public string FinalVariablesFileName { get; init; } = "final.json";
}

/// <summary>String and list variables captured after a plan run (element refs are not included).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AutomationPlanExecutionSnapshot
{
    public IReadOnlyDictionary<string, string> Strings { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> StringLists { get; }

    public AutomationPlanExecutionSnapshot(IReadOnlyDictionary<string, string> strings, IReadOnlyDictionary<string, IReadOnlyList<string>> stringLists)
    {
        Strings = strings;
        StringLists = stringLists;
    }

    /// <inheritdoc />
    public override string ToString() => $"AutomationPlanExecutionSnapshot strings={Strings.Count}, stringLists={StringLists.Count}";
}