namespace Lyo.ContentThreatScan;

/// <summary>Compact scan result: totals come from additive contributions, then a score cap.</summary>
public sealed class ContentThreatAssessment
{
    /// <summary>Value compared to suspect/threat cutoffs after the <see cref="ContentThreatAssessmentOptions" /> cap.</summary>
    public decimal DispositionScore { get; }

    public decimal HeuristicScore { get; }

    public decimal ExternalScore { get; }

    public bool IntelConfirmedMalicious { get; }

    public IReadOnlyList<ContentThreatContribution> Contributions { get; }

    public ContentThreatAssessment(
        decimal dispositionScore,
        decimal heuristicScore,
        decimal externalScore,
        bool intelConfirmedMalicious,
        IReadOnlyList<ContentThreatContribution> contributions)
    {
        DispositionScore = dispositionScore;
        HeuristicScore = heuristicScore;
        ExternalScore = externalScore;
        IntelConfirmedMalicious = intelConfirmedMalicious;
        Contributions = contributions;
    }

    /// <summary>Roll up flattened contributions, treating heuristic vs external by category.</summary>
    public static ContentThreatAssessment FromContributions(IReadOnlyList<ContentThreatContribution> contributions, bool intelConfirmedMalicious, decimal dispositionScoreCap)
    {
        decimal h = 0m, e = 0m;
        foreach (var c in contributions) {
            if (c.Category is ContentThreatCategory.Reputation or ContentThreatCategory.AntiMalwareEngine)
                e += c.Points;
            else
                h += c.Points;
        }

        var uncapped = h + e;
        var capped = dispositionScoreCap < 0 ? uncapped : Math.Min(dispositionScoreCap, uncapped);
        return new(capped, h, e, intelConfirmedMalicious, contributions.Count == 0 ? contributions : NormalizeOrder(contributions));
    }

    private static ContentThreatContribution[] NormalizeOrder(IReadOnlyList<ContentThreatContribution> list)
    {
        var arr = new ContentThreatContribution[list.Count];
        for (var i = 0; i < arr.Length; i++)
            arr[i] = list[i];

        Array.Sort(arr, CompareByCategoryThenRule);
        return arr;
    }

    private static int CompareByCategoryThenRule(ContentThreatContribution a, ContentThreatContribution b)
    {
        var c = ((int)a.Category).CompareTo((int)b.Category);
        return c != 0 ? c : string.CompareOrdinal(a.RuleId, b.RuleId);
    }
}