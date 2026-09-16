using Lyo.Exceptions.Models;
using Lyo.Web.Automation.Abstractions;

namespace Lyo.Web.Automation.Plan;

internal sealed class NullAutomationPlanDataSink : IAutomationPlanDataSink
{
    public Task UpsertJsonAsync(string targetName, string jsonPayload, CancellationToken ct)
        => throw new ConfigurationException($"No {nameof(IAutomationPlanDataSink)} is registered. Configure one before using upsert steps.");
}

internal sealed class NullAutomationPlanFileStorage : IAutomationPlanFileStorage
{
    public Task<IReadOnlyList<string>> UploadDirectoryAsync(string sourceDirectory, string destinationPrefix, CancellationToken ct)
        => throw new ConfigurationException($"No {nameof(IAutomationPlanFileStorage)} is registered. Configure one before using upload steps.");

    /// <inheritdoc />
    public Task<string?> SaveSnapshotAsync(byte[] png, Guid runId, Guid stepId, CancellationToken ct) => Task.FromResult<string?>(null);

    /// <inheritdoc />
    public Task<string?> SaveAsync(byte[] data, string fileName, string pathPrefix, string contentType, CancellationToken ct) => Task.FromResult<string?>(null);

    /// <inheritdoc />
    public Task<byte[]?> ReadSnapshotAsync(string fileId, CancellationToken ct) => Task.FromResult<byte[]?>(null);
}