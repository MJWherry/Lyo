namespace Lyo.ContentThreatScan.Abstractions;

/// <summary>Optional external caches emitting reputation-weighted contributions.</summary>
public interface IContentThreatReputationPipeline
{
    Task<ExternalReputationEnvelope> InspectAsync(ContentThreatReputationRequest request, ContentThreatScanContext context, CancellationToken ct = default);
}