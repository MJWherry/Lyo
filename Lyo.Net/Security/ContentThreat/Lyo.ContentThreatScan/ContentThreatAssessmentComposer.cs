using Lyo.Exceptions;

namespace Lyo.ContentThreatScan;

/// <summary>Unifies heuristic + optional external contributions into a capped assessment consumed by callers.</summary>
public static class ContentThreatAssessmentComposer
{
    public static ContentThreatAssessment Compose(
        IReadOnlyList<ContentThreatContribution> heuristicContributions,
        ExternalReputationEnvelope external,
        ContentThreatAssessmentOptions options)
    {
        ArgumentHelpers.ThrowIfNull(heuristicContributions);
        ArgumentHelpers.ThrowIfNull(external);
        ArgumentHelpers.ThrowIfNull(options);
        var combinedCount = heuristicContributions.Count + external.Contributions.Count;
        ContentThreatContribution[] merged;
        if (combinedCount == 0)
            merged = Array.Empty<ContentThreatContribution>();
        else {
            merged = new ContentThreatContribution[combinedCount];
            var ix = 0;
            foreach (var c in heuristicContributions)
                merged[ix++] = c;

            foreach (var c in external.Contributions)
                merged[ix++] = c;
        }

        var confirmed = external.IntelConfirmedMalicious;
        return ContentThreatAssessment.FromContributions(merged, confirmed, options.DispositionScoreCap);
    }
}