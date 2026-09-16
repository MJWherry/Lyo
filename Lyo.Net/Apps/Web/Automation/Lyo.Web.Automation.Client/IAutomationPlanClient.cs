using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Client;

/// <summary>Starts a plan run and reads its status, log, and viewport snapshots. The browser stays on the API host.</summary>
public interface IAutomationPlanClient
{
    /// <summary>Posts the plan. The response returns when the run is queued, not when the browser finishes.</summary>
    Task<AutomationRunAccepted> StartAsync(AutomationRunReq request, CancellationToken ct = default);

    /// <summary>Current status and, after a successful finish, the string tables.</summary>
    Task<AutomationRunRes?> GetAsync(Guid runId, CancellationToken ct = default);

    /// <summary>Cancels the run token.</summary>
    Task CancelAsync(Guid runId, CancellationToken ct = default);

    /// <summary>UTF-8 transcript from <c>logs/run.log</c>.</summary>
    Task<string> GetLogAsync(Guid runId, CancellationToken ct = default);

    /// <summary>PNG file names captured for the run.</summary>
    Task<IReadOnlyList<string>> ListSnapshotsAsync(Guid runId, CancellationToken ct = default);

    /// <summary>One viewport PNG.</summary>
    Task<byte[]> GetSnapshotAsync(Guid runId, string name, CancellationToken ct = default);

    /// <summary>Viewport PNG stored for one finished step. Empty when that step has no file id.</summary>
    Task<byte[]> GetStepScreenshotAsync(Guid runId, int index, CancellationToken ct = default);

    /// <summary>Names of the plans stored on the API host.</summary>
    Task<IReadOnlyList<AutomationPlanSummary>> ListPlansAsync(CancellationToken ct = default);

    /// <summary>Creates an empty named plan. The browser stays on the API host.</summary>
    Task<AutomationPlan> CreatePlanAsync(string name, CancellationToken ct = default);

    /// <summary>The stored plan, or null when the name is not on the host.</summary>
    Task<AutomationPlan?> GetPlanAsync(string name, CancellationToken ct = default);

    /// <summary>The stored plan, or null when the id is not on the host.</summary>
    Task<AutomationPlan?> GetPlanByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Replaces the stored plan. Invalid JSON is rejected by the host and the previous plan stays.</summary>
    Task<AutomationPlan> SavePlanAsync(string name, AutomationPlan plan, CancellationToken ct = default);
}
