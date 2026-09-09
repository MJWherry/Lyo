namespace Lyo.ContentThreatScan;

/// <summary>External lookups folded into additive contributions that merge with heuristic scoring.</summary>
public sealed class ExternalReputationEnvelope(IReadOnlyList<ContentThreatContribution> contributions, bool intelConfirmedMalicious)
{
    public static ExternalReputationEnvelope Empty { get; } = new([], false);

    public IReadOnlyList<ContentThreatContribution> Contributions { get; } = contributions;

    public bool IntelConfirmedMalicious { get; } = intelConfirmedMalicious;
}