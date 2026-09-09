using Lyo.ContentThreatScan.Abstractions;

namespace Lyo.ContentThreatScan;

/// <summary>Built-in pipeline that does nothing.</summary>
public sealed class NullContentThreatReputationPipeline : IContentThreatReputationPipeline
{
    public static NullContentThreatReputationPipeline Instance { get; } = new();

    public Task<ExternalReputationEnvelope> InspectAsync(ContentThreatReputationRequest request, ContentThreatScanContext context, CancellationToken ct = default)
        => Task.FromResult(ExternalReputationEnvelope.Empty);
}