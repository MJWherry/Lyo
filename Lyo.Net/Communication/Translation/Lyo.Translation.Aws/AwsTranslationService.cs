using System.Diagnostics;
using Amazon;
using Amazon.Translate;
using Amazon.Translate.Model;
using Lyo.Common.Metadata.Extensions;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Translation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.Translation.TranslationErrorCodes;

namespace Lyo.Translation.Aws;

/// <summary>Amazon Translate-backed implementation of text translation between languages.</summary>
/// <remarks>
/// <para>
/// Thread-safe: instance fields are readonly, and bulk work uses the base class's thread-safe collections and synchronization
/// primitives.
/// </para>
/// </remarks>
public sealed class AwsTranslationService : TranslationServiceBase
{
    private readonly bool _ownsTranslateClient;
    private readonly IAmazonTranslate _translateClient;

    /// <summary>Constructs an AwsTranslationService.</summary>
    /// <param name="options">AWS Translate settings. Must not be null.</param>
    /// <param name="logger">Optional logger for service activity.</param>
    /// <param name="metrics">Optional metrics sink for translation work.</param>
    /// <param name="translateClient">Optional Amazon Translate client. When omitted a client is built from options.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public AwsTranslationService(AwsTranslationOptions options, ILogger<AwsTranslationService>? logger = null, IMetrics? metrics = null, IAmazonTranslate? translateClient = null)
        : base(options, logger ?? NullLoggerFactory.Instance.CreateLogger<AwsTranslationService>(), metrics)
    {
        if (translateClient != null) {
            _translateClient = translateClient;
            _ownsTranslateClient = false;
        }
        else {
            _translateClient = CreateTranslateClient(options);
            _ownsTranslateClient = true;
        }

        // Replace base metric names with the AWS Translate set
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

        // Fall back to the default credential chain (IAM role, environment variables, and so on)
        return new AmazonTranslateClient(config);
    }

    /// <summary>Frees unmanaged resources, and managed ones when <paramref name="disposing" /> is true.</summary>
    /// <param name="disposing">True to free managed and unmanaged resources; false for unmanaged only.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _ownsTranslateClient)
            _translateClient.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>Translates via AWS Translate.</summary>
    /// <param name="request">Request holding the text and language choices.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>TranslationResult with success or failure and the translated text when successful.</returns>
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
            // AWS Translate expects ISO 639-1 (two-letter) codes
            var targetLanguage = request.TargetLanguageCode.Iso6391 ?? request.TargetLanguageCode.Bcp47.Split('-')[0];
            var sourceLanguage = request.SourceLanguage?.Iso6391 ?? request.SourceLanguage?.Bcp47.Split('-')[0];
            var translateRequest = new TranslateTextRequest { Text = request.Text, SourceLanguageCode = sourceLanguage ?? "auto", TargetLanguageCode = targetLanguage };
            var response = await _translateClient.TranslateTextAsync(translateRequest, ct).ConfigureAwait(false);
            sw.Stop();
            var detectedLanguage = LanguageCodeInfo.Unknown;
            if (!string.IsNullOrWhiteSpace(response.SourceLanguageCode) && response.SourceLanguageCode != "auto") {
                // Convert the ISO 639-1 code to LanguageCodeInfo via the extension
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

    /// <summary>Infers language of <paramref name="text" /> through AWS Translate.</summary>
    /// <remarks>
    /// <para>
    /// AWS Translate has no dedicated language-detection API. This calls TranslateText with source "auto" and target "en" so the
    /// source language is reported. The translated string is thrown away.
    /// </para>
    /// </remarks>
    /// <param name="text">Sample whose language should be identified.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Detected language code, or Unknown when detection fails.</returns>
    public override async Task<LanguageCodeInfo> DetectLanguageAsync(string text, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        using var timer = Metrics.StartTimer(MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageDuration)]);
        try {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(text);
            ArgumentHelpers.ThrowIfNotInRange(text.Length, 1, Options.MaxTextLength, nameof(text));

            // AWS Translate has no DetectDominantLanguage API
            // TranslateText with source "auto" is used to learn the language
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

    /// <summary>Checks that AWS Translate can be reached.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the provider answers; otherwise false.</returns>
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