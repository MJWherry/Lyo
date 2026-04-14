using System.Diagnostics;
using Amazon;
using Amazon.Translate;
using Amazon.Translate.Model;
using Lyo.Common;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Translation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.Translation.TranslationErrorCodes;

namespace Lyo.Translation.Aws;

/// <summary>AWS Translate service implementation using Amazon Translate for translating text between languages.</summary>
/// <remarks>
/// <para>
/// This class is thread-safe. All instance fields are readonly and operations use thread-safe collections and synchronization primitives from the base class for bulk
/// operations.
/// </para>
/// </remarks>
public sealed class AwsTranslationService : TranslationServiceBase
{
    private readonly AwsTranslationOptions _options;
    private readonly bool _ownsTranslateClient;
    private readonly IAmazonTranslate _translateClient;

    /// <summary>Initializes a new instance of the AwsTranslationService class.</summary>
    /// <param name="options">The AWS Translate configuration options. Must not be null.</param>
    /// <param name="logger">Optional logger instance for logging operations.</param>
    /// <param name="metrics">Optional metrics instance for tracking translation operations.</param>
    /// <param name="translateClient">Optional Amazon Translate client. If not provided, a new instance will be created from options.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public AwsTranslationService(AwsTranslationOptions options, ILogger<AwsTranslationService>? logger = null, IMetrics? metrics = null, IAmazonTranslate? translateClient = null)
        : base(options, logger ?? NullLoggerFactory.Instance.CreateLogger<AwsTranslationService>(), metrics)
    {
        _options = options;
        if (translateClient != null) {
            _translateClient = translateClient;
            _ownsTranslateClient = false;
        }
        else {
            _translateClient = CreateTranslateClient(options);
            _ownsTranslateClient = true;
        }

        // Override base metric names with AWS Translate-specific ones
        MetricNames[nameof(Translation.Constants.Metrics.TranslateDuration)] = Constants.Metrics.TranslateDuration;
        MetricNames[nameof(Translation.Constants.Metrics.TranslateSuccess)] = Constants.Metrics.TranslateSuccess;
        MetricNames[nameof(Translation.Constants.Metrics.TranslateFailure)] = Constants.Metrics.TranslateFailure;
        MetricNames[nameof(Translation.Constants.Metrics.BulkTranslateDuration)] = Constants.Metrics.BulkTranslateDuration;
        MetricNames[nameof(Translation.Constants.Metrics.BulkTranslateTotal)] = Constants.Metrics.BulkTranslateTotal;
        MetricNames[nameof(Translation.Constants.Metrics.BulkTranslateSuccess)] = Constants.Metrics.BulkTranslateSuccess;
        MetricNames[nameof(Translation.Constants.Metrics.BulkTranslateFailure)] = Constants.Metrics.BulkTranslateFailure;
        MetricNames[nameof(Translation.Constants.Metrics.BulkTranslateLastDurationMs)] = Constants.Metrics.BulkTranslateLastDurationMs;
        MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageDuration)] = Constants.Metrics.DetectLanguageDuration;
        MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageSuccess)] = Constants.Metrics.DetectLanguageSuccess;
        MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageFailure)] = Constants.Metrics.DetectLanguageFailure;
    }

    private static IAmazonTranslate CreateTranslateClient(AwsTranslationOptions options)
    {
        var config = new AmazonTranslateConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region) };
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            config.ServiceURL = options.ServiceUrl;

        if (!string.IsNullOrWhiteSpace(options.AccessKeyId) && !string.IsNullOrWhiteSpace(options.SecretAccessKey))
            return new AmazonTranslateClient(options.AccessKeyId, options.SecretAccessKey, config);

        // Use default credentials (IAM role, environment variables, etc.)
        return new AmazonTranslateClient(config);
    }

    /// <summary>Releases the unmanaged resources used by the AwsTranslationService and optionally releases the managed resources.</summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _ownsTranslateClient)
            _translateClient?.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>Translates text using AWS Translate.</summary>
    /// <param name="request">The translation request containing text and options.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A TranslationResult indicating success or failure with translated text.</returns>
    protected override async Task<TranslationResult> TranslateCoreAsync(TranslationRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Text, nameof(request.Text));
            ArgumentHelpers.ThrowIfNotInRange(request.Text.Length, 1, Options.MaxTextLength, nameof(request.Text));
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Error translating text");
            return TranslationResult.FromException(ex, request, sw.Elapsed, TranslateFailed);
        }

        try {
            // AWS Translate uses ISO 639-1 (2-letter) codes
            var targetLanguage = request.TargetLanguageCode.Iso6391 ?? request.TargetLanguageCode.Bcp47?.Split('-')[0];
            var sourceLanguage = request.SourceLanguage?.Iso6391 ?? request.SourceLanguage?.Bcp47?.Split('-')[0];
            var translateRequest = new TranslateTextRequest { Text = request.Text, SourceLanguageCode = sourceLanguage ?? "auto", TargetLanguageCode = targetLanguage };
            var response = await _translateClient.TranslateTextAsync(translateRequest, ct).ConfigureAwait(false);
            sw.Stop();
            var detectedLanguage = LanguageCodeInfo.Unknown;
            if (!string.IsNullOrWhiteSpace(response.SourceLanguageCode) && response.SourceLanguageCode != "auto") {
                // Map ISO 639-1 code to LanguageCodeInfo using extension method
                detectedLanguage = response.SourceLanguageCode.FromISO639_1();
            }

            Logger.LogDebug(
                "Successfully translated text using AWS Translate. Source: {SourceLanguage}, Target: {TargetLanguage}, Detected: {DetectedLanguage}", sourceLanguage ?? "auto",
                targetLanguage, detectedLanguage != LanguageCodeInfo.Unknown ? detectedLanguage.Iso6393 ?? detectedLanguage.Iso6391 ?? "unknown" : "unknown");

            return TranslationResult.FromSuccess(
                request, response.TranslatedText, sw.Elapsed, detectedLanguage != LanguageCodeInfo.Unknown ? detectedLanguage : null, response.ResponseMetadata.RequestId,
                "Text translated successfully");
        }
        catch (OperationCanceledException) {
            sw.Stop();
            Logger.LogWarning("AWS Translate translation was cancelled");
            return TranslationResult.FromException(new OperationCanceledException(ct), request, sw.Elapsed, OperationCancelled);
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Error translating text with AWS Translate");
            return TranslationResult.FromException(ex, request, sw.Elapsed, TranslateFailed);
        }
    }

    /// <summary>Detects the language of the provided text using AWS Translate.</summary>
    /// <remarks>
    /// <para>
    /// AWS Translate doesn't have a separate language detection API. This method uses TranslateText with "auto" as the source language and "en" as the target language to detect the
    /// source language. The translation result is discarded.
    /// </para>
    /// </remarks>
    /// <param name="text">The text to detect the language for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The detected language code, or Unknown if detection fails.</returns>
    public override async Task<LanguageCodeInfo> DetectLanguageAsync(string text, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        using var timer = Metrics.StartTimer(MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageDuration)]);
        try {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(text, nameof(text));
            ArgumentHelpers.ThrowIfNotInRange(text.Length, 1, Options.MaxTextLength, nameof(text));

            // AWS Translate doesn't have a separate DetectDominantLanguage API
            // We use TranslateText with "auto" source language to detect the language
            var request = new TranslateTextRequest {
                Text = text, SourceLanguageCode = "auto", TargetLanguageCode = "en" // Use English as target for detection
            };

            var response = await _translateClient.TranslateTextAsync(request, ct).ConfigureAwait(false);
            sw.Stop();
            if (!string.IsNullOrWhiteSpace(response.SourceLanguageCode) && response.SourceLanguageCode != "auto") {
                var detectedLanguage = response.SourceLanguageCode.FromISO639_1();
                Logger.LogDebug("Successfully detected language using AWS Translate. Language: {Language}", detectedLanguage.Iso6393 ?? detectedLanguage.Iso6391 ?? "unknown");
                Metrics.IncrementCounter(MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageSuccess)]);
                return detectedLanguage;
            }

            Logger.LogWarning("AWS Translate did not return a detected language");
            Metrics.IncrementCounter(MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageFailure)]);
            return LanguageCodeInfo.Unknown;
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Error detecting language with AWS Translate");
            Metrics.IncrementCounter(MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageFailure)]);
            Metrics.RecordError(MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageDuration)], ex);
            return LanguageCodeInfo.Unknown;
        }
    }

    /// <summary>Tests the connection to AWS Translate.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    public override async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try {
            var request = new ListLanguagesRequest();
            await _translateClient.ListLanguagesAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("Successfully tested AWS Translate connection");
            return true;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error testing AWS Translate connection");
            return false;
        }
    }
}