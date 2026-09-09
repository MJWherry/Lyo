using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Lyo.Common.Json;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Extensions;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Translation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.Translation.TranslationErrorCodes;

namespace Lyo.Translation.Google;

/// <summary>Google Cloud Translation API-backed implementation of text translation between languages.</summary>
/// <remarks>
/// <para>
/// Thread-safe: instance fields are readonly, and bulk work uses the base class's thread-safe collections and synchronization
/// primitives.
/// </para>
/// </remarks>
public sealed class GoogleTranslationService : TranslationServiceBase
{
    private static readonly JsonSerializerOptions JsonOptions = LyoJsonSerializerOptions.Create();

    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private readonly GoogleTranslationOptions _options;

    /// <summary>Constructs a GoogleTranslationService.</summary>
    /// <param name="options">Google Translate settings. Must not be null.</param>
    /// <param name="logger">Optional logger for service activity.</param>
    /// <param name="metrics">Optional metrics sink for translation work.</param>
    /// <param name="httpClient">Optional HTTP client. When omitted a new client is created.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public GoogleTranslationService(GoogleTranslationOptions options, ILogger<GoogleTranslationService>? logger = null, IMetrics? metrics = null, HttpClient? httpClient = null)
        : base(options, logger ?? NullLoggerFactory.Instance.CreateLogger<GoogleTranslationService>(), metrics)
    {
        _options = options;
        _httpClient = httpClient ?? new HttpClient();
        _baseUrl = options.ApiEndpoint.TrimEnd('/');

        // Replace base metric names with the Google Translate set
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

    /// <summary>Frees unmanaged resources, and managed ones when <paramref name="disposing" /> is true.</summary>
    /// <param name="disposing">True to free managed and unmanaged resources; false for unmanaged only.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _httpClient.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>Translates via Google Translate.</summary>
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
            // Google Translate expects ISO 639-1 (two-letter) codes
            var targetLanguage = request.TargetLanguageCode.Iso6391 ?? request.TargetLanguageCode.Bcp47.Split('-')[0];
            var sourceLanguage = request.SourceLanguage?.Iso6391 ?? request.SourceLanguage?.Bcp47.Split('-')[0];
            var apiKey = _options.ApiKey;
            OperationHelpers.ThrowIfNullOrWhiteSpace(apiKey, "Google Translate API key is required. Set ApiKey in GoogleTranslationOptions.");
            var url = $"{_baseUrl}?key={Uri.EscapeDataString(apiKey)}";
            var requestBody = new { q = request.Text, target = targetLanguage, source = sourceLanguage };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, FileTypeInfo.Json.MimeType);
            var response = await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);
            sw.Stop();
#if NETSTANDARD2_0
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#else
            var responseContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#endif
            if (!response.IsSuccessStatusCode) {
                Logger.LogError("Google Translate API error: {StatusCode} - {Response}", response.StatusCode, responseContent);
                return TranslationResult.FromException(
                    new HttpRequestException($"Status: {response.StatusCode}, Response: {responseContent}"), request, sw.Elapsed, TranslateFailed);
            }

            var result = JsonSerializer.Deserialize<GoogleTranslateResponse>(responseContent, JsonOptions);
            if (result?.Data?.Translations == null || result.Data.Translations.Count == 0) {
                Logger.LogWarning("Google Translate returned no translations");
                return TranslationResult.FromException(new InvalidOperationException("No translations in response"), request, sw.Elapsed, TranslateFailed);
            }

            var translation = result.Data.Translations[0];
            var detectedLanguage = LanguageCodeInfo.Unknown;
            if (!translation.DetectedSourceLanguage.IsNullOrWhitespace())
                detectedLanguage = translation.DetectedSourceLanguage.FromISO639_1();

            Logger.LogDebug(
                "Successfully translated text using Google Translate. Source: {SourceLanguage}, Target: {TargetLanguage}, Detected: {DetectedLanguage}", sourceLanguage ?? "auto",
                targetLanguage, detectedLanguage != LanguageCodeInfo.Unknown ? detectedLanguage.Iso6393 ?? detectedLanguage.Iso6391 ?? "unknown" : "unknown");

            return TranslationResult.FromSuccess(
                request, translation.TranslatedText, sw.Elapsed, detectedLanguage != LanguageCodeInfo.Unknown ? detectedLanguage : null, null, "Text translated successfully");
        }
        catch (OperationCanceledException) {
            sw.Stop();
            Logger.LogWarning("Google Translate translation was cancelled");
            return TranslationResult.FromException(new OperationCanceledException(ct), request, sw.Elapsed, OperationCancelled);
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Error translating text with Google Translate");
            return TranslationResult.FromException(ex, request, sw.Elapsed, TranslateFailed);
        }
    }

    /// <summary>Infers language of <paramref name="text" /> through Google Translate.</summary>
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
            var apiKey = _options.ApiKey;
            OperationHelpers.ThrowIfNullOrWhiteSpace(apiKey, "Google Translate API key is required. Set ApiKey in GoogleTranslationOptions.");
            var url = $"{_baseUrl}/detect?key={Uri.EscapeDataString(apiKey)}";
            var requestBody = new { q = text };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, FileTypeInfo.Json.MimeType);
            var response = await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);
            sw.Stop();
#if NETSTANDARD2_0
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#else
            var responseContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#endif
            if (!response.IsSuccessStatusCode) {
                Logger.LogError("Google Translate API error: {StatusCode} - {Response}", response.StatusCode, responseContent);
                Metrics.IncrementCounter(MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageFailure)]);
                return LanguageCodeInfo.Unknown;
            }

            var result = JsonSerializer.Deserialize<GoogleDetectLanguageResponse>(responseContent, JsonOptions);
            if (result?.Data?.Detections == null || result.Data.Detections.Count == 0 || result.Data.Detections[0].Count == 0) {
                Logger.LogWarning("Google Translate did not return a detected language");
                Metrics.IncrementCounter(MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageFailure)]);
                return LanguageCodeInfo.Unknown;
            }

            var detectedLanguage = result.Data.Detections[0][0].Language.FromISO639_1();
            Logger.LogDebug("Successfully detected language using Google Translate. Language: {Language}", detectedLanguage.Iso6393 ?? detectedLanguage.Iso6391 ?? "unknown");
            Metrics.IncrementCounter(MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageSuccess)]);
            return detectedLanguage;
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Error detecting language with Google Translate");
            Metrics.IncrementCounter(MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageFailure)]);
            Metrics.RecordError(MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageDuration)], ex);
            return LanguageCodeInfo.Unknown;
        }
    }

    /// <summary>Checks that Google Translate can be reached.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the provider answers; otherwise false.</returns>
    public override async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try {
            // Probe the API by detecting the language of a short English word
            var result = await DetectLanguageAsync("hello", ct).ConfigureAwait(false);
            Logger.LogDebug("Successfully tested Google Translate connection");
            return result != LanguageCodeInfo.Unknown;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error testing Google Translate connection");
            return false;
        }
    }

    [DebuggerDisplay("{ToString(),nq}")]
    internal sealed record GoogleTranslateResponse(GoogleTranslateData? Data)
    {
        public override string ToString() => Data?.ToString() ?? "";
    }

    [DebuggerDisplay("{ToString(),nq}")]
    internal sealed record GoogleTranslateData(IReadOnlyList<GoogleTranslation> Translations)
    {
        public override string ToString() => $"Translations: {Translations.Count}";
    }

    [DebuggerDisplay("{ToString(),nq}")]
    internal sealed record GoogleTranslation(string TranslatedText, string? DetectedSourceLanguage)
    {
        public override string ToString() => $"{(DetectedSourceLanguage.IsNullOrEmpty() ? "" : $"Detected {DetectedSourceLanguage}, ")}{TranslatedText}";
    }

    [DebuggerDisplay("{ToString(),nq}")]
    internal sealed record GoogleDetectLanguageResponse(GoogleDetectLanguageData? Data)
    {
        public override string ToString() => Data?.ToString() ?? "";
    }

    [DebuggerDisplay("{ToString(),nq}")]
    internal sealed record GoogleDetectLanguageData(IReadOnlyList<IReadOnlyList<GoogleDetection>> Detections)
    {
        public override string ToString() => $"Detections: {Detections.Count}";
    }

    [DebuggerDisplay("{ToString(),nq}")]
    internal sealed record GoogleDetection(string Language, double Confidence)
    {
        public override string ToString() => $"{Language}: {Confidence}";
    }
}