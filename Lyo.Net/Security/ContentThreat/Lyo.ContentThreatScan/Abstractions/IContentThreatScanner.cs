namespace Lyo.ContentThreatScan.Abstractions;

/// <summary>Collects heuristic SQL/script-pattern contributions from a capped UTF-8 sample.</summary>
public interface IContentThreatScanner
{
    Task<IReadOnlyList<ContentThreatContribution>> CollectHeuristicContributionsAsync(
        ReadOnlyMemory<byte> sampledBytes,
        ContentThreatScanContext context,
        CancellationToken cancellationToken = default);
}
