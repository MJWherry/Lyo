using Lyo.Api.Models.Common.Response;
using Lyo.Drift.Models.Request;
using Lyo.Drift.Models.Response;

namespace Lyo.Drift.Client;

/// <summary>HTTP client for the Drift collector: instance upsert, snapshot/diff/change ingest, and lookups.</summary>
public interface IDriftClient
{
    /// <summary>Upserts an agent instance by <see cref="DriftInstanceReq.InstanceKey" />.</summary>
    Task<CreateResult<DriftInstanceRes>> UpsertInstanceAsync(DriftInstanceReq request, CancellationToken ct = default);

    /// <summary>Sends a heartbeat. Throws <c>ApiException</c> with status 404 when the instance row is gone.</summary>
    Task<DriftInstanceRes> HeartbeatAsync(Guid instanceId, DriftInstanceHeartbeatReq? request = null, CancellationToken ct = default);

    /// <summary>Marks the instance Stopped.</summary>
    Task<DriftInstanceRes> StopAsync(Guid instanceId, CancellationToken ct = default);

    /// <summary>Posts a file-tree or system-info structure snapshot.</summary>
    Task<CreateResult<DriftSnapshotRes>> PostSnapshotAsync(DriftSnapshotReq request, CancellationToken ct = default);

    /// <summary>Posts a precomputed diff.</summary>
    Task<CreateResult<DriftDiffRes>> PostDiffAsync(DriftDiffReq request, CancellationToken ct = default);

    /// <summary>Posts a live change batch.</summary>
    Task<IReadOnlyList<DriftChangeRes>> PostChangesAsync(DriftChangeBatchReq request, CancellationToken ct = default);

    /// <summary>Gets one stored snapshot.</summary>
    Task<DriftSnapshotRes?> GetSnapshotAsync(Guid snapshotId, CancellationToken ct = default);

    /// <summary>Gets one stored instance.</summary>
    Task<DriftInstanceRes?> GetInstanceAsync(Guid instanceId, CancellationToken ct = default);

    /// <summary>Asks the server to diff two stored snapshots of the same kind.</summary>
    Task<CreateResult<DriftDiffRes>> DiffAgainstAsync(Guid snapshotId, Guid otherId, CancellationToken ct = default);
}
