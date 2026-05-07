using Lyo.Web.Automation.Abstractions;

namespace Lyo.Web.Automation.Plan;

internal sealed class NullAutomationPlanDataSink : IAutomationPlanDataSink
{
    public Task UpsertJsonAsync(string targetName, string jsonPayload, CancellationToken ct)
        => throw new InvalidOperationException($"No {nameof(IAutomationPlanDataSink)} is registered. Configure one before using upsert steps.");
}

internal sealed class NullAutomationPlanFileStorage : IAutomationPlanFileStorage
{
    public Task<IReadOnlyList<string>> UploadDirectoryAsync(string sourceDirectory, string destinationPrefix, CancellationToken ct)
        => throw new InvalidOperationException($"No {nameof(IAutomationPlanFileStorage)} is registered. Configure one before using upload steps.");
}