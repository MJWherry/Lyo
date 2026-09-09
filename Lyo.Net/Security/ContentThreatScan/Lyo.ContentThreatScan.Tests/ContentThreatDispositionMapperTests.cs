namespace Lyo.ContentThreatScan.Tests;

public sealed class ContentThreatDispositionMapperTests
{
    [Fact]
    public void Resolve_IntelConfirmationSwitch_RespectsOption()
    {
        var flagged = ContentThreatAssessment.FromContributions([], true, 999m);
        ContentThreatAssessmentOptions forceThreatIntel = new() { SuspectThreshold = decimal.MaxValue, ThreatThreshold = decimal.MaxValue, ForceThreatOnConfirmedIntel = true };
        Assert.Equal(ContentThreatDisposition.Threat, ContentThreatDispositionMapper.Resolve(flagged, forceThreatIntel));
        ContentThreatAssessmentOptions ignoreIntelThreat = new() { SuspectThreshold = decimal.MaxValue, ThreatThreshold = decimal.MaxValue, ForceThreatOnConfirmedIntel = false };
        Assert.Equal(ContentThreatDisposition.Clean, ContentThreatDispositionMapper.Resolve(flagged, ignoreIntelThreat));
    }
}
