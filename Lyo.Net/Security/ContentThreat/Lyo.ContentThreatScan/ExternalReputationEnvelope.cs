namespace Lyo.ContentThreatScan;

/// <summary>External lookups grouped into additive contributions merged with heuristic scoring.</summary>
public sealed class ExternalReputationEnvelope
{
    public static ExternalReputationEnvelope Empty { get; } = new(Array.Empty<ContentThreatContribution>(), false);

    public ExternalReputationEnvelope(IReadOnlyList<ContentThreatContribution> contributions, bool intelConfirmedMalicious)
    {
        Contributions = contributions ?? Array.Empty<ContentThreatContribution>();
        IntelConfirmedMalicious = intelConfirmedMalicious;
    }

    public IReadOnlyList<ContentThreatContribution> Contributions { get; }

    public bool IntelConfirmedMalicious { get; }
}
