using Lyo.ClamAv.Client;
using Lyo.ContentThreatScan;
using Lyo.ContentThreatScan.Abstractions;
using Lyo.Exceptions;
using Lyo.Http.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.ContentThreatScan.ClamAv;

/// <summary>
/// Posts content to <see cref="IClamAvClient" /> and returns an <see cref="ExternalReputationEnvelope" />. Register <c>AddClamAvClient</c> before
/// <c>AddClamAvContentThreatScan</c>. Pass the envelope to <see cref="ContentThreatAssessmentComposer" />.
/// </summary>
public sealed class ClamAvContentThreatScan : IContentThreatReputationPipeline
{
    private readonly IClamAvClient _client;
    private readonly ILogger<ClamAvContentThreatScan> _log;
    private readonly ClamAvContentThreatScanOptions _options;

    /// <summary>Creates the scan. <paramref name="logger" /> is optional.</summary>
    public ClamAvContentThreatScan(IClamAvClient client, ClamAvContentThreatScanOptions options, ILogger<ClamAvContentThreatScan>? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(client);
        ArgumentHelpers.ThrowIfNull(options);
        _client = client;
        _options = options;
        _log = logger ?? NullLogger<ClamAvContentThreatScan>.Instance;
    }

    /// <summary>Scans <paramref name="content" />. The multipart file name is <see cref="ContentThreatScanContext.OriginalFileName" />, or <c>upload.bin</c> when that is missing.</summary>
    public async Task<ExternalReputationEnvelope> ScanAsync(Stream content, ContentThreatScanContext context, CancellationToken cancellationToken = default)
    {
        ArgumentHelpers.ThrowIfNull(content);
        ArgumentHelpers.ThrowIfNull(context);
        var originalName = context.OriginalFileName;
        var fileName = string.IsNullOrWhiteSpace(originalName) ? "upload.bin" : originalName!;
        try {
            var result = await _client.ScanAsync(content, fileName, cancellationToken).ConfigureAwait(false);
            return FromResult(result);
        }
        catch (LyoHttpException ex) {
            _log.LogWarning(ex, "ClamAV scan failed.");
            return Failure();
        }
    }

    /// <summary>Scans <see cref="ContentThreatReputationRequest.LimitedSamplePrefix" /> when those bytes exist. An empty prefix does not call the client.</summary>
    public async Task<ExternalReputationEnvelope> InspectAsync(ContentThreatReputationRequest request, ContentThreatScanContext context, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(context);
        if (request.LimitedSamplePrefix is not { Length: > 0 } prefix)
            return ExternalReputationEnvelope.Empty;

        using var stream = new MemoryStream(prefix.ToArray(), writable: false);
        return await ScanAsync(stream, context, ct).ConfigureAwait(false);
    }

    private ExternalReputationEnvelope FromResult(ClamScanResult result)
    {
        if (result.Status == ClamScanStatus.Clean)
            return ExternalReputationEnvelope.Empty;

        if (result.Status == ClamScanStatus.Infected) {
            ContentThreatContribution hit = new("clamav.detected", ContentThreatCategory.AntiMalwareEngine, _options.DetectionPoints, result.Signature);
            return new([hit], _options.MarksIntelConfirmed);
        }

        _log.LogWarning("ClamAV scan returned status {Status}.", result.Status);
        return Failure();
    }

    private ExternalReputationEnvelope Failure()
        => _options.FailureDisposition switch {
            ClamAvScanFailureDisposition.TreatAsSuspect => new([new ContentThreatContribution("clamav.failure", ContentThreatCategory.Reputation, _options.SuspectBump)], false),
            ClamAvScanFailureDisposition.ImmediateThreatBump => new(
                [new ContentThreatContribution("clamav.failure.threat", ContentThreatCategory.Reputation, _options.ThreatBump)], false),
            _ => ExternalReputationEnvelope.Empty
        };
}
