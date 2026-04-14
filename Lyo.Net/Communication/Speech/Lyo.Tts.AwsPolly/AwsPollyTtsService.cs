using System.Diagnostics;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Tts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.Tts.TtsErrorCodes;

namespace Lyo.Tts.AwsPolly;

/// <summary>AWS Polly TTS service implementation using Amazon Polly for converting text to speech.</summary>
/// <remarks>
/// <para>
/// This class is thread-safe. All instance fields are readonly and operations use thread-safe collections and synchronization primitives from the base class for bulk
/// operations.
/// </para>
/// </remarks>
public sealed class AwsPollyTtsService : TtsServiceBase<AwsPollyTtsRequest>
{
    private readonly AwsPollyOptions _options;
    private readonly bool _ownsPollyClient;
    private readonly IAmazonPolly _pollyClient;

    /// <summary>Initializes a new instance of the AwsPollyTtsService class.</summary>
    /// <param name="options">The AWS Polly configuration options. Must not be null.</param>
    /// <param name="logger">Optional logger instance for logging operations.</param>
    /// <param name="metrics">Optional metrics instance for tracking TTS operations.</param>
    /// <param name="pollyClient">Optional Amazon Polly client. If not provided, a new instance will be created from options.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public AwsPollyTtsService(AwsPollyOptions options, ILogger<AwsPollyTtsService>? logger = null, IMetrics? metrics = null, IAmazonPolly? pollyClient = null)
        : base(options, logger ?? NullLoggerFactory.Instance.CreateLogger<AwsPollyTtsService>(), metrics)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        _options = options;
        if (pollyClient != null) {
            _pollyClient = pollyClient;
            _ownsPollyClient = false;
        }
        else {
            _pollyClient = CreatePollyClient(options);
            _ownsPollyClient = true;
        }

        // Override base metric names with AWS Polly-specific ones
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

        // Use default credentials (IAM role, environment variables, etc.)
        return new AmazonPollyClient(config);
    }

    /// <summary>Releases the unmanaged resources used by the AwsPollyTtsService and optionally releases the managed resources.</summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _ownsPollyClient)
            _pollyClient?.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>Synthesizes text to speech using AWS Polly.</summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="voiceId">Optional voice ID. If not provided, uses the default voice from options or language code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A TtsResult indicating success or failure with audio data.</returns>
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

    /// <summary>Synthesizes text to speech using AWS Polly.</summary>
    /// <remarks>
    /// <para>
    /// <strong>Important:</strong> In AWS Polly, when a VoiceId is specified, the voice's language is already determined by the voice itself. Each voice (e.g., "Astrid" for
    /// Swedish, "Joanna" for English US) has a fixed language/locale.
    /// </para>
    /// <para>
    /// The LanguageCode parameter in AWS Polly's SynthesizeSpeechRequest is primarily used for voice filtering/selection, not for overriding the voice's language. When a VoiceId is
    /// provided, the LanguageCode is ignored or may cause conflicts.
    /// </para>
    /// <para>
    /// Therefore, this method only sets LanguageCode when no VoiceId is specified, allowing AWS Polly to select an appropriate voice based on the language code. When VoiceId is
    /// provided, only the VoiceId is used (the voice's inherent language is used).
    /// </para>
    /// </remarks>
    /// <param name="request">The TTS request containing text and options.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A TtsResult indicating success or failure with audio data.</returns>
    protected override async Task<TtsResult<AwsPollyTtsRequest>> SynthesizeCoreAsync(AwsPollyTtsRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Text, nameof(request.Text));
            ArgumentHelpers.ThrowIfNotInRange(request.Text.Length, 1, Options.MaxTextLength, nameof(request.Text));
            request.VoiceId ??= _options.DefaultVoiceIdEnum;
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Error validating TTS request");
            return TtsResult<AwsPollyTtsRequest>.FromException(ex, request, sw.Elapsed, SynthesizeFailed);
        }

        try {
            // Use default output format if not specified
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

    /// <summary>Tests the connection to AWS Polly.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the connection is successful, false otherwise.</returns>
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