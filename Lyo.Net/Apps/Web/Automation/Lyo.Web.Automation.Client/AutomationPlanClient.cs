using System.Text;
using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Exceptions;
using Lyo.Web.Automation.Core;
using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Client;

/// <summary>Typed facade over the automation run API. Sit it on any <see cref="IApiClient" />.</summary>
public sealed class AutomationPlanClient : IAutomationPlanClient
{
    private readonly IApiClient _api;
    private readonly string? _routePrefix;

    /// <summary>Constructs the client. Null <paramref name="options" /> keeps routes relative to the inner client's base address.</summary>
    public AutomationPlanClient(IApiClient apiClient, AutomationPlanClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(apiClient);
        _api = apiClient;
        _routePrefix = options?.RoutePrefix;
    }

    /// <inheritdoc />
    public Task<AutomationRunAccepted> StartAsync(AutomationRunReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PostAsAsync<AutomationRunReq, AutomationRunAccepted>(Route(Constants.Rest.Runs), request, ct: ct);
    }

    /// <inheritdoc />
    public Task<AutomationRunRes?> GetAsync(Guid runId, CancellationToken ct = default)
        => _api.GetAsAsync<AutomationRunRes>(Route(Constants.Rest.Run(runId)), ct: ct);

    /// <inheritdoc />
    public Task CancelAsync(Guid runId, CancellationToken ct = default)
        => _api.PostAsAsync<object>(Route(Constants.Rest.Cancel(runId)), ct: ct);

    /// <inheritdoc />
    public async Task<string> GetLogAsync(Guid runId, CancellationToken ct = default)
    {
        var bytes = await _api.GetFileAsync(Route(Constants.Rest.Log(runId)), ct: ct).ConfigureAwait(false);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListSnapshotsAsync(Guid runId, CancellationToken ct = default)
    {
        var names = await _api.GetAsAsync<List<string>>(Route(Constants.Rest.Snapshots(runId)), ct: ct).ConfigureAwait(false);
        return names ?? [];
    }

    /// <inheritdoc />
    public Task<byte[]> GetSnapshotAsync(Guid runId, string name, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        return _api.GetFileAsync(Route(Constants.Rest.Snapshot(runId, name)), ct: ct);
    }

    /// <inheritdoc />
    public Task<byte[]> GetStepScreenshotAsync(Guid runId, int index, CancellationToken ct = default)
        => _api.GetFileAsync(Route(Constants.Rest.StepScreenshot(runId, index)), ct: ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AutomationPlanSummary>> ListPlansAsync(CancellationToken ct = default)
    {
        var plans = await _api.GetAsAsync<List<AutomationPlanSummary>>(Route(Constants.Rest.Plans), ct: ct).ConfigureAwait(false);
        return plans ?? [];
    }

    /// <inheritdoc />
    public Task<AutomationPlan> CreatePlanAsync(string name, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        return _api.PostAsAsync<AutomationPlanCreateReq, AutomationPlan>(Route(Constants.Rest.Plans), new AutomationPlanCreateReq { Name = name }, ct: ct);
    }

    /// <inheritdoc />
    public Task<AutomationPlan?> GetPlanAsync(string name, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        return _api.GetAsAsync<AutomationPlan>(Route(Constants.Rest.Plan(name)), ct: ct);
    }

    /// <inheritdoc />
    public Task<AutomationPlan?> GetPlanByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsAsync<AutomationPlan>(Route(Constants.Rest.PlanById(id)), ct: ct);

    /// <inheritdoc />
    public Task<AutomationPlan> SavePlanAsync(string name, AutomationPlan plan, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNull(plan);
        return _api.PutAsAsync<AutomationPlan, AutomationPlan>(Route(Constants.Rest.Plan(name)), plan, ct: ct);
    }

    private string Route(string relativePath) => ApiRouteBuilder.Build(_routePrefix, relativePath);
}
