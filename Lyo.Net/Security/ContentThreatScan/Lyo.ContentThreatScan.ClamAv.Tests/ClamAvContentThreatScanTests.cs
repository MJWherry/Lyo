using Lyo.ClamAv.Client;
using Lyo.Http.Client;

namespace Lyo.ContentThreatScan.ClamAv.Tests;

public sealed class ClamAvContentThreatScanTests
{
    [Fact]
    public async Task ScanAsync_Clean_ReturnsEmptyEnvelope()
    {
        var fake = new FakeClamAvClient { Result = new(ClamScanStatus.Clean, null) };
        var scan = Scan(fake, new());
        var envelope = await scan.ScanAsync(Payload(), new(), TestCt);
        Assert.Empty(envelope.Contributions);
        Assert.False(envelope.IntelConfirmedMalicious);
        Assert.Equal("upload.bin", fake.FileName);
    }

    [Fact]
    public async Task ScanAsync_Infected_AddsEngineContribution()
    {
        var fake = new FakeClamAvClient { Result = new(ClamScanStatus.Infected, "Win.Test.EICAR_HDB-1") };
        var scan = Scan(fake, new() { DetectionPoints = 40m, MarksIntelConfirmed = true });
        var envelope = await scan.ScanAsync(Payload(), new("dropper.bin"), TestCt);
        var hit = Assert.Single(envelope.Contributions);
        Assert.Equal("clamav.detected", hit.RuleId);
        Assert.Equal(ContentThreatCategory.AntiMalwareEngine, hit.Category);
        Assert.Equal(40m, hit.Points);
        Assert.Equal("Win.Test.EICAR_HDB-1", hit.Detail);
        Assert.True(envelope.IntelConfirmedMalicious);
        Assert.Equal("dropper.bin", fake.FileName);
        var assessmentOptions = new ContentThreatAssessmentOptions();
        var assessment = ContentThreatAssessmentComposer.Compose([], envelope, assessmentOptions);
        Assert.Equal(ContentThreatDisposition.Threat, ContentThreatDispositionMapper.Resolve(assessment, assessmentOptions));
    }

    [Fact]
    public async Task ScanAsync_EngineError_Ignore_ReturnsEmpty()
    {
        var fake = new FakeClamAvClient { Result = new(ClamScanStatus.Error, "nope") };
        var scan = Scan(fake, new() { FailureDisposition = ClamAvScanFailureDisposition.Ignore });
        var envelope = await scan.ScanAsync(Payload(), new("sample.bin"), TestCt);
        Assert.Empty(envelope.Contributions);
        Assert.False(envelope.IntelConfirmedMalicious);
        Assert.Equal(1, fake.Calls);
    }

    [Fact]
    public async Task ScanAsync_TransportFailure_TreatAsSuspect_AddsReputationBump()
    {
        var fake = new FakeClamAvClient { Error = new LyoHttpException(503, "down") };
        var scan = Scan(fake, new() { FailureDisposition = ClamAvScanFailureDisposition.TreatAsSuspect });
        var envelope = await scan.ScanAsync(Payload(), new("sample.bin"), TestCt);
        var hit = Assert.Single(envelope.Contributions);
        Assert.Equal("clamav.failure", hit.RuleId);
        Assert.Equal(ContentThreatCategory.Reputation, hit.Category);
        Assert.Equal(16m, hit.Points);
        Assert.False(envelope.IntelConfirmedMalicious);
    }

    [Fact]
    public async Task ScanAsync_TransportFailure_ImmediateThreatBump_AddsThreatPoints()
    {
        var fake = new FakeClamAvClient { Error = new LyoHttpException(503, "down") };
        var scan = Scan(fake, new() { FailureDisposition = ClamAvScanFailureDisposition.ImmediateThreatBump });
        var envelope = await scan.ScanAsync(Payload(), new("sample.bin"), TestCt);
        var hit = Assert.Single(envelope.Contributions);
        Assert.Equal("clamav.failure.threat", hit.RuleId);
        Assert.Equal(ContentThreatCategory.Reputation, hit.Category);
        Assert.Equal(132m, hit.Points);
        Assert.False(envelope.IntelConfirmedMalicious);
    }

    [Fact]
    public async Task InspectAsync_MissingPrefix_DoesNotCallClient()
    {
        var fake = new FakeClamAvClient { Result = new(ClamScanStatus.Infected, "Win.Test.EICAR_HDB-1") };
        var scan = Scan(fake, new());
        var envelope = await scan.InspectAsync(new(new byte[32]), new("sample.bin"), TestCt);
        Assert.Equal(0, fake.Calls);
        Assert.Empty(envelope.Contributions);
        Assert.False(envelope.IntelConfirmedMalicious);
    }

    [Fact]
    public async Task InspectAsync_WithPrefix_ScansThoseBytes()
    {
        var sample = new byte[] { 9, 8, 7, 6 };
        var fake = new FakeClamAvClient { Result = new(ClamScanStatus.Clean, null) };
        var scan = Scan(fake, new());
        await scan.InspectAsync(new(new byte[32], sample), new("sample.bin"), TestCt);
        Assert.Equal(1, fake.Calls);
        Assert.Equal(sample, fake.Bytes);
        Assert.Equal("sample.bin", fake.FileName);
    }

    private static ClamAvContentThreatScan Scan(FakeClamAvClient fake, ClamAvContentThreatScanOptions options) => new(fake, options);

    private static MemoryStream Payload() => new([1, 2, 3, 4]);

    private sealed class FakeClamAvClient : IClamAvClient
    {
        public ClamScanResult? Result { get; set; }

        public Exception? Error { get; set; }

        public string? FileName { get; private set; }

        public byte[]? Bytes { get; private set; }

        public int Calls { get; private set; }

        public async Task<ClamScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
        {
            Calls++;
            FileName = fileName;
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            Bytes = buffer.ToArray();
            if (Error != null)
                throw Error;

            return Result!;
        }

        public Task<string> GetVersionAsync(CancellationToken cancellationToken = default) => Task.FromResult("unused");
    }
}
