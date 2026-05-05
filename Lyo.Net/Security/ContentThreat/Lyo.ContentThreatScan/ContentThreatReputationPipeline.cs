namespace Lyo.ContentThreatScan;

/// <summary>Optional external caches emitting reputation-weighted contributions.</summary>
public interface IContentThreatReputationPipeline
{
    Task<ExternalReputationEnvelope> InspectAsync(ContentThreatReputationRequest request, ContentThreatScanContext context, CancellationToken ct = default);
}

/// <summary>Default no-op pipeline.</summary>
public sealed class NullContentThreatReputationPipeline : IContentThreatReputationPipeline
{
    public static NullContentThreatReputationPipeline Instance { get; } = new();

    public Task<ExternalReputationEnvelope> InspectAsync(ContentThreatReputationRequest request, ContentThreatScanContext context, CancellationToken ct = default)
        => Task.FromResult(ExternalReputationEnvelope.Empty);
}
