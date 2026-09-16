namespace Lyo.Web.Automation.Abstractions;

/// <summary>Sends local files produced during a plan run to a backing storage service.</summary>
public interface IAutomationPlanFileStorage
{
    /// <summary>Uploads all files under a directory and yields stored locations/keys.</summary>
    Task<IReadOnlyList<string>> UploadDirectoryAsync(string sourceDirectory, string destinationPrefix, CancellationToken ct);

    /// <summary>
    /// Stores one viewport PNG at <c>Runs/{runId}/steps/{stepId}/{stepId}.png</c>.
    /// <paramref name="stepId" /> is the step execution id. Returns the storage id, or null when no backing store is configured.
    /// </summary>
    Task<string?> SaveSnapshotAsync(byte[] png, Guid runId, Guid stepId, CancellationToken ct);

    /// <summary>
    /// Stores bytes under a run or step prefix from <c>AutomationRunStoragePaths</c>.
    /// A run file uses <c>Runs/{runId}</c>. An extraction for one step uses <c>Runs/{runId}/steps/{stepId}</c>.
    /// Returns the storage id, or null when no backing store is configured.
    /// </summary>
    Task<string?> SaveAsync(byte[] data, string fileName, string pathPrefix, string contentType, CancellationToken ct);

    /// <summary>Reads a PNG previously stored by <see cref="SaveSnapshotAsync" />. Null when the id is unknown.</summary>
    Task<byte[]?> ReadSnapshotAsync(string fileId, CancellationToken ct);
}
