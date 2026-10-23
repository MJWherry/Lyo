using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Models;

/// <summary>Body of <c>POST Automation/Plan</c>. <see cref="Name" /> is one path segment.</summary>
public sealed class AutomationPlanCreateReq
{
    /// <summary>Display name stored on the plan and used in the plan URL.</summary>
    public string? Name { get; set; }
}

/// <summary>One row from <c>GET Automation/Plan</c>.</summary>
public sealed class AutomationPlanSummary
{
    /// <summary>Plan id. Empty when the row was stored before ids existed.</summary>
    public Guid Id { get; set; }

    /// <summary>Plan name.</summary>
    public string Name { get; set; } = "";
}

/// <summary>Body of <c>POST Automation/Run</c>. <see cref="Plan" /> is the document to execute.</summary>
public sealed class AutomationRunReq
{
    /// <summary>Plan JSON. Required.</summary>
    public AutomationPlan? Plan { get; set; }

    /// <summary>Browser to launch. Null uses Chromium.</summary>
    public AutomationBrowserKind? BrowserKind { get; set; }

    /// <summary>When null, the API launches headless.</summary>
    public bool? Headless { get; set; }
}

/// <summary>Status strings returned by the run API.</summary>
public static class AutomationRunStatus
{
    /// <summary>Accepted and not yet inside the runner.</summary>
    public const string Queued = "queued";

    /// <summary>The runner is executing the plan.</summary>
    public const string Running = "running";

    /// <summary>The runner returned a snapshot.</summary>
    public const string Completed = "completed";

    /// <summary>The runner threw.</summary>
    public const string Failed = "failed";

    /// <summary>The caller cancelled the run token.</summary>
    public const string Cancelled = "cancelled";
}

/// <summary><c>202</c> body from <c>POST Automation/Run</c>.</summary>
public sealed class AutomationRunAccepted
{
    /// <summary>Id used by the get, cancel, log, and snapshot routes.</summary>
    public Guid RunId { get; set; }

    /// <summary>Always <see cref="AutomationRunStatus.Queued" /> at accept time.</summary>
    public string Status { get; set; } = AutomationRunStatus.Queued;
}

/// <summary><c>GET Automation/Run/{id}</c> body. String tables are present once the run has finished.</summary>
public sealed class AutomationRunRes
{
    /// <summary>Run id.</summary>
    public Guid RunId { get; set; }

    /// <summary>One of the <see cref="AutomationRunStatus" /> values.</summary>
    public string Status { get; set; } = AutomationRunStatus.Queued;

    /// <summary>Exception message when <see cref="Status" /> is <see cref="AutomationRunStatus.Failed" />.</summary>
    public string? Error { get; set; }

    /// <summary>Final string variables. Null until the run finishes successfully.</summary>
    public IReadOnlyDictionary<string, string>? Strings { get; set; }

    /// <summary>Final string-list variables. Null until the run finishes successfully.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? StringLists { get; set; }

    /// <summary>Steps that have finished. Grows while the run is still going.</summary>
    public IReadOnlyList<AutomationRunStepRes> Steps { get; set; } = [];
}

/// <summary>One finished step on <see cref="AutomationRunRes" />.</summary>
public sealed class AutomationRunStepRes
{
    /// <summary>Zero-based index in the plan.</summary>
    public int Index { get; set; }

    /// <summary>Optional step name.</summary>
    public string? Name { get; set; }

    /// <summary>JSON <c>type</c> discriminator.</summary>
    public string Type { get; set; } = "";

    /// <summary>Outcome name, such as <c>Success</c> or <c>Failed</c>.</summary>
    public string Outcome { get; set; } = "";

    /// <summary>How long the step ran.</summary>
    public long DurationMs { get; set; }

    /// <summary>Exception message when the step failed.</summary>
    public string? Error { get; set; }

    /// <summary>File id of the viewport PNG, when storage kept one.</summary>
    public string? ScreenshotFileId { get; set; }

    /// <summary>Variables and context bag after the step.</summary>
    public AutomationPlanContextSnapshot? Context { get; set; }
}