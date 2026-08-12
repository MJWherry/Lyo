namespace Lyo.ContentThreatScan;

/// <summary>External lookups grouped into additive contributions merged with heuristic scoring.</summary>
public sealed class ExternalReputationEnvelope(IReadOnlyList<ContentThreatContribution> contributions, bool intelConfirmedMalicious)
{
    public static ExternalReputationEnvelope Empty { get; } = new([], false);

    public IReadOnlyList<ContentThreatContribution> Contributions { get; } = contributions;

    public bool IntelConfirmedMalicious { get; } = intelConfirmedMalicious;
}