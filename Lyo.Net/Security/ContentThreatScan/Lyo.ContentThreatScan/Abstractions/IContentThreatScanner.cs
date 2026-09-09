namespace Lyo.ContentThreatScan.Abstractions;

/// <summary>Gathers heuristic SQL/script-pattern hits from a capped UTF-8 sample.</summary>
public interface IContentThreatScanner
{
    Task<IReadOnlyList<ContentThreatContribution>> CollectHeuristicContributionsAsync(
        ReadOnlyMemory<byte> sampledBytes,
        ContentThreatScanContext context,
        CancellationToken cancellationToken = default);
}