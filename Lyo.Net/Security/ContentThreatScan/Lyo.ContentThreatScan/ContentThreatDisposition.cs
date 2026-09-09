using Lyo.Exceptions;

namespace Lyo.ContentThreatScan;

/// <summary>Turns the summed score plus intel confirmation hints into disposition bands callers consume.</summary>
public enum ContentThreatDisposition
{
    Clean = 0,
    Suspect = 1,
    Threat = 2
}

/// <summary>Configurable map from a numeric score to <see cref="ContentThreatDisposition" />.</summary>
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