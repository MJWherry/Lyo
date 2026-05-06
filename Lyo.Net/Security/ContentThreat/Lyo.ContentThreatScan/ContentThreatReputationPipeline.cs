using Lyo.ContentThreatScan.Abstractions;

namespace Lyo.ContentThreatScan;

/// <summary>Default no-op pipeline.</summary>
public sealed class NullContentThreatReputationPipeline : IContentThreatReputationPipeline
{
    public static NullContentThreatReputationPipeline Instance { get; } = new();

    public Task<ExternalReputationEnvelope> InspectAsync(ContentThreatReputationRequest request, ContentThreatScanContext context, CancellationToken ct = default)
        => Task.FromResult(ExternalReputationEnvelope.Empty);
}
