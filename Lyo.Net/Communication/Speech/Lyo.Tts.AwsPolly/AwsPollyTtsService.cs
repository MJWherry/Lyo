using System.Diagnostics;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Extensions;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Tts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.Tts.TtsErrorCodes;

namespace Lyo.Tts.AwsPolly;

/// <summary>Amazon Polly-backed TTS that turns text into speech.</summary>
/// <remarks>
/// <para>
/// Thread-safe: instance fields are readonly, and bulk work uses the base class's thread-safe collections and synchronization
/// primitives.
/// </para>
/// </remarks>
public sealed class AwsPollyTtsService : TtsServiceBase<AwsPollyTtsRequest>
{
    private readonly AwsPollyOptions _options;
    private readonly bool _ownsPollyClient;
    private readonly IAmazonPolly _pollyClient;

    /// <summary>Constructs an AwsPollyTtsService.</summary>
    /// <param name="options">Polly settings. Must not be null.</param>
    /// <param name="logger">Optional logger for service activity.</param>
    /// <param name="metrics">Optional metrics sink for TTS work.</param>
    /// <param name="pollyClient">Optional IAmazonPolly. When omitted, one is built from options.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public AwsPollyTtsService(AwsPollyOptions options, ILogger<AwsPollyTtsService>? logger = null, IMetrics? metrics = null, IAmazonPolly? pollyClient = null)
        : base(options, logger ?? NullLoggerFactory.Instance.CreateLogger<AwsPollyTtsService>(), metrics)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _options = options;
        if (pollyClient != null) {
            _pollyClient = pollyClient;
            _ownsPollyClient = false;
        }
        else {
            _pollyClient = CreatePollyClient(options);
            _ownsPollyClient = true;
        }

        // Replace base metric names with the AWS Polly set
        MetricNames[nameof(Tts.Constants.Metrics.SynthesizeDuration)] = Constants.Metrics.SynthesizeDuration;
        MetricNames[nameof(Tts.Constants.Metrics.SynthesizeSuccess)] = Constants.Metrics.SynthesizeSuccess;
        MetricNames[nameof(Tts.Constants.Metrics.SynthesizeFailure)] = Constants.Metrics.SynthesizeFailure;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeDuration)] = Constants.Metrics.BulkSynthesizeDuration;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeTotal)] = Constants.Metrics.BulkSynthesizeTotal;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeSuccess)] = Constants.Metrics.BulkSynthesizeSuccess;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeFailure)] = Constants.Metrics.BulkSynthesizeFailure;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeLastDurationMs)] = Constants.Metrics.BulkSynthesizeLastDurationMs;
    }

    private static IAmazonPolly CreatePollyClient(AwsPollyOptions options)
    {
        var config = new AmazonPollyConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region) };
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            config.ServiceURL = options.ServiceUrl;

        if (!string.IsNullOrWhiteSpace(options.AccessKeyId) && !string.IsNullOrWhiteSpace(options.SecretAccessKey))
            return new AmazonPollyClient(options.AccessKeyId, options.SecretAccessKey, config);

        // Fall back to the default credential chain (IAM role, environment, and so on)
        return new AmazonPollyClient(config);
    }

    /// <summary>Frees unmanaged resources, and managed ones when <paramref name="disposing" /> is true.</summary>
    /// <param name="disposing">True to free managed and unmanaged resources; false for unmanaged only.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _ownsPollyClient)
            _pollyClient.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>Speaks text through AWS Polly.</summary>
    /// <param name="text">Text to synthesize.</param>
    /// <param name="voiceId">Optional voice. When omitted, uses the options default or the language code.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>TtsResult with success or failure and audio bytes when successful.</returns>
    public override async Task<TtsResult<AwsPollyTtsRequest>> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default)
    {
        AwsPollyVoiceId? finalVoiceId = _options.DefaultVoiceIdEnum;
        if (!string.IsNullOrWhiteSpace(voiceId) && Enum.TryParse<AwsPollyVoiceId>(voiceId, out var id))
            finalVoiceId = id;

        var outputFormat = _options.DefaultOutputFormat ?? AudioFormat.Mp3;
        var languageCode = _options.DefaultLanguageCode;
        var request = new AwsPollyTtsRequest(text, finalVoiceId, languageCode, outputFormat);
        return await SynthesizeAsync(request, ct).ConfigureAwait(false);
    }

    /// <summary>Speaks via AWS Polly from a request object.</summary>
    /// <remarks>
    /// <para>
    /// <strong>Important:</strong> Polly ties language to the chosen VoiceId. A voice such as "Astrid" (Swedish) or
    /// "Joanna" (English US) has a fixed locale.
    /// </para>
    /// <para>
    /// LanguageCode on SynthesizeSpeechRequest is mainly for picking a voice, not for changing that voice's language. With a VoiceId
    /// present, LanguageCode is ignored or can conflict.
    /// </para>
    /// <para>
    /// This method therefore sets LanguageCode only when VoiceId is omitted, so Polly can pick a voice for that language. When VoiceId is
    /// supplied, only VoiceId is sent (the voice's own language applies).
    /// </para>
    /// </remarks>
    /// <param name="request">Request holding the text and options.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>TtsResult with success or failure and audio bytes when successful.</returns>
    protected override async Task<TtsResult<AwsPollyTtsRequest>> SynthesizeCoreAsync(AwsPollyTtsRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Text);
            ArgumentHelpers.ThrowIfNotInRange(request.Text.Length, 1, Options.MaxTextLength);
            request.VoiceId ??= _options.DefaultVoiceIdEnum;
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Error validating TTS request");
            return TtsResult<AwsPollyTtsRequest>.FromException(ex, request, sw.Elapsed, SynthesizeFailed);
        }

        try {
            // Fall back to the default output format when none is set
            var outputFormat = request.OutputFormat != AudioFormat.Unknown ? request.OutputFormat : _options.DefaultOutputFormat ?? AudioFormat.Mp3;
            var synthesizeRequest = new SynthesizeSpeechRequest { Text = request.Text, OutputFormat = outputFormat.GetStringValue(), VoiceId = request.VoiceId!.Value.ToString() };
            var response = await _pollyClient.SynthesizeSpeechAsync(synthesizeRequest, ct).ConfigureAwait(false);
            sw.Stop();
            if (response.AudioStream == null) {
                Logger.LogWarning("AWS Polly returned null audio stream");
                return TtsResult<AwsPollyTtsRequest>.FromException(new InvalidOperationException("Null audio stream received"), request, sw.Elapsed, SynthesizeFailed);
            }

            byte[] audioData;
            using (var audioStream = response.AudioStream) {
                using (var memoryStream = new MemoryStream()) {
#if NETSTANDARD2_0
                    await audioStream.CopyToAsync(memoryStream).ConfigureAwait(false);
#else
                    await audioStream.CopyToAsync(memoryStream, ct).ConfigureAwait(false);
#endif
                    audioData = memoryStream.ToArray();
                }
            }

            if (audioData.Length == 0) {
                Logger.LogWarning("AWS Polly returned empty audio data");
                return TtsResult<AwsPollyTtsRequest>.FromException(new InvalidOperationException("Empty audio data received"), request, sw.Elapsed, SynthesizeFailed);
            }

            Logger.LogDebug(
                "Successfully synthesized text to speech using AWS Polly. Voice: {VoiceId}, Format: {Format}, Length: {Length} bytes", request.VoiceId?.ToString() ?? "auto",
                outputFormat.GetStringValue(), audioData.Length);

            return TtsResult<AwsPollyTtsRequest>.FromSuccess(request, audioData, sw.Elapsed, response.ResponseMetadata.RequestId, "Speech synthesized successfully");
        }
        catch (OperationCanceledException) {
            sw.Stop();
            Logger.LogWarning("AWS Polly TTS synthesis was cancelled");
            return TtsResult<AwsPollyTtsRequest>.FromException(new OperationCanceledException(ct), request, sw.Elapsed, OperationCancelled);
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Error synthesizing text to speech with AWS Polly");
            return TtsResult<AwsPollyTtsRequest>.FromException(ex, request, sw.Elapsed, SynthesizeFailed);
        }
    }

    /// <summary>Checks that AWS Polly can be reached.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when Polly answers; otherwise false.</returns>
    public override async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try {
            var request = new DescribeVoicesRequest();
            await _pollyClient.DescribeVoicesAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("Successfully tested AWS Polly connection");
            return true;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error testing AWS Polly connection");
            return false;
        }
    }
}