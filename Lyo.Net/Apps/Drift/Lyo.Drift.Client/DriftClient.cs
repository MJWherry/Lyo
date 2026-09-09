using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Drift.Models.Request;
using Lyo.Drift.Models.Response;
using Lyo.Exceptions;
using DriftRoutes = Lyo.Drift.Models.Constants.Rest.Drift;

namespace Lyo.Drift.Client;

/// <summary>Typed facade over the Drift collector API.</summary>
public sealed class DriftClient : IDriftClient
{
    private readonly IApiClient _client;
    private readonly string? _routePrefix;

    /// <summary>Builds a client on <paramref name="apiClient" />.</summary>
    public DriftClient(IApiClient apiClient, DriftClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(apiClient);
        _client = apiClient;
        _routePrefix = options?.RoutePrefix;
    }

    /// <inheritdoc />
    public Task<CreateResult<DriftInstanceRes>> UpsertInstanceAsync(DriftInstanceReq request, CancellationToken ct = default)
        => _client.PostAsAsync<DriftInstanceReq, CreateResult<DriftInstanceRes>>(Route(DriftRoutes.InstanceUpsert), request, ct: ct);

    /// <inheritdoc />
    public Task<DriftInstanceRes> HeartbeatAsync(Guid instanceId, DriftInstanceHeartbeatReq? request = null, CancellationToken ct = default)
        => _client.PatchAsAsync<DriftInstanceHeartbeatReq, DriftInstanceRes>(Route(DriftRoutes.InstanceHeartbeat(instanceId)), request, ct: ct);

    /// <inheritdoc />
    public Task<DriftInstanceRes> StopAsync(Guid instanceId, CancellationToken ct = default)
        => _client.PostAsAsync<DriftInstanceRes>(Route(DriftRoutes.InstanceStop(instanceId)), ct: ct);

    /// <inheritdoc />
    public Task<CreateResult<DriftSnapshotRes>> PostSnapshotAsync(DriftSnapshotReq request, CancellationToken ct = default)
        => _client.PostAsAsync<DriftSnapshotReq, CreateResult<DriftSnapshotRes>>(Route(DriftRoutes.Snapshots), request, ct: ct);

    /// <inheritdoc />
    public Task<CreateResult<DriftDiffRes>> PostDiffAsync(DriftDiffReq request, CancellationToken ct = default)
        => _client.PostAsAsync<DriftDiffReq, CreateResult<DriftDiffRes>>(Route(DriftRoutes.Diffs), request, ct: ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<DriftChangeRes>> PostChangesAsync(DriftChangeBatchReq request, CancellationToken ct = default)
        => _client.PostAsAsync<DriftChangeBatchReq, IReadOnlyList<DriftChangeRes>>(Route(DriftRoutes.Changes), request, ct: ct);

    /// <inheritdoc />
    public Task<DriftSnapshotRes?> GetSnapshotAsync(Guid snapshotId, CancellationToken ct = default)
        => _client.GetAsAsync<DriftSnapshotRes>(Route($"{DriftRoutes.Snapshots}/{snapshotId}"), ct: ct);

    /// <inheritdoc />
    public Task<DriftInstanceRes?> GetInstanceAsync(Guid instanceId, CancellationToken ct = default)
        => _client.GetAsAsync<DriftInstanceRes>(Route($"{DriftRoutes.Instances}/{instanceId}"), ct: ct);

    /// <inheritdoc />
    public Task<CreateResult<DriftDiffRes>> DiffAgainstAsync(Guid snapshotId, Guid otherId, CancellationToken ct = default)
        => _client.PostAsAsync<CreateResult<DriftDiffRes>>(Route(DriftRoutes.DiffAgainst(snapshotId, otherId)), ct: ct);

    private string Route(string relative) => ApiRouteBuilder.Build(_routePrefix, relative);
}
