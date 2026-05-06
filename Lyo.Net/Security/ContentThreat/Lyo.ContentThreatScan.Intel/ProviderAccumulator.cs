namespace Lyo.ContentThreatScan.Intel;

internal sealed record ProviderAccumulator(
    List<ContentThreatContribution> Contributions,
    bool IntelConfirmedMalicious)
{
    public static ProviderAccumulator Merge(ProviderAccumulator a, ProviderAccumulator b)
    {
        var list = new List<ContentThreatContribution>(a.Contributions.Count + b.Contributions.Count);
        list.AddRange(a.Contributions);
        list.AddRange(b.Contributions);
        return new(list, a.IntelConfirmedMalicious || b.IntelConfirmedMalicious);
    }

    public ExternalReputationEnvelope Finish() =>
        new(Contributions, IntelConfirmedMalicious);
}