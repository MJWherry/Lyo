using Lyo.Exceptions;

namespace Lyo.ContentThreatScan;

/// <summary>Maps summed disposition score plus intel confirmation hints into disposition bands used by callers.</summary>
public enum ContentThreatDisposition
{
    Clean = 0,
    Suspect = 1,
    Threat = 2
}

/// <summary>Configurable mapping from numerical score onto <see cref="ContentThreatDisposition" />.</summary>
public static class ContentThreatDispositionMapper
{
    public static ContentThreatDisposition Resolve(ContentThreatAssessment assessment, ContentThreatAssessmentOptions options)
    {
        ArgumentHelpers.ThrowIfNull(assessment);
        ArgumentHelpers.ThrowIfNull(options);
        if (options.ForceThreatOnConfirmedIntel && assessment.IntelConfirmedMalicious)
            return ContentThreatDisposition.Threat;

        if (assessment.DispositionScore < options.SuspectThreshold)
            return ContentThreatDisposition.Clean;

        if (assessment.DispositionScore < options.ThreatThreshold)
            return ContentThreatDisposition.Suspect;

        return ContentThreatDisposition.Threat;
    }
}