using Lyo.ContentThreatScan.Intel;

namespace Lyo.ContentThreatScan.Tests;

public sealed class ContentThreatAssessmentComposerTests
{
    [Fact]
    public void Compose_ExternalPointsExplode_CapsDispositionScore()
    {
        ExternalReputationEnvelope extEnvelope = new([new("reputation.case", ContentThreatCategory.Reputation, 500m)], false);
        var capped = ContentThreatAssessmentComposer.Compose([], extEnvelope, new() { DispositionScoreCap = 42m });
        Assert.Equal(42m, capped.DispositionScore);
    }
}
