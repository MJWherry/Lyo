using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Lyo.Common;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Translation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.Translation.TranslationErrorCodes;

namespace Lyo.Translation.Google;

/// <summary>Google Translate service implementation using Google Cloud Translation API for translating text between languages.</summary>
/// <remarks>
/// <para>
/// This class is thread-safe. All instance fields are readonly and operations use thread-safe collections and synchronization primitives from the base class for bulk
/// operations.
/// </para>
/// </remarks>
public sealed class GoogleTranslationService : TranslationServiceBase
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private readonly GoogleTranslationOptions _options;

    /// <summary>Initializes a new instance of the GoogleTranslationService class.</summary>
    /// <param name="options">The Google Translate configuration options. Must not be null.</param>
    /// <param name="logger">Optional logger instance for logging operations.</param>
    /// <param name="metrics">Optional metrics instance for tracking translation operations.</param>
    /// <param name="httpClient">Optional HTTP client. If not provided, a new instance will be created.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public GoogleTranslationService(GoogleTranslationOptions options, ILogger<GoogleTranslationService>? logger = null, IMetrics? metrics = null, HttpClient? httpClient = null)
        : base(options, logger ?? NullLoggerFactory.Instance.CreateLogger<GoogleTranslationService>(), metrics)
    {
        _options = options;
        _httpClient = httpClient ?? new HttpClient();
        _baseUrl = string.IsNullOrWhiteSpace(options.ApiEndpoint) ? "https://translation.googleapis.com/language/translate/v2" : options.ApiEndpoint.TrimEnd('/');

        // Override base metric names with Google Translate-specific ones
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

    /// <summary>Releases the unmanaged resources used by the GoogleTranslationService and optionally releases the managed resources.</summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _httpClient != null)
            _httpClient.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>Translates text using Google Translate.</summary>
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
            // Google Translate uses ISO 639-1 (2-letter) codes
            var targetLanguage = request.TargetLanguageCode.Iso6391 ?? request.TargetLanguageCode.Bcp47?.Split('-')[0];
            var sourceLanguage = request.SourceLanguage?.Iso6391 ?? request.SourceLanguage?.Bcp47?.Split('-')[0];
            var apiKey = _options.ApiKey;
            OperationHelpers.ThrowIfNullOrWhiteSpace(apiKey, "Google Translate API key is required. Set ApiKey in GoogleTranslationOptions.");
            var url = $"{_baseUrl}?key={Uri.EscapeDataString(apiKey)}";
            var requestBody = new { q = request.Text, target = targetLanguage, source = sourceLanguage };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
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

            var result = JsonSerializer.Deserialize<GoogleTranslateResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result?.Data?.Translations == null || result.Data.Translations.Count == 0) {
                Logger.LogWarning("Google Translate returned no translations");
                return TranslationResult.FromException(new InvalidOperationException("No translations in response"), request, sw.Elapsed, TranslateFailed);
            }

            var translation = result.Data.Translations[0];
            var detectedLanguage = LanguageCodeInfo.Unknown;
            if (!string.IsNullOrWhiteSpace(translation.DetectedSourceLanguage))
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

    /// <summary>Detects the language of the provided text using Google Translate.</summary>
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
            var apiKey = _options.ApiKey;
            OperationHelpers.ThrowIfNullOrWhiteSpace(apiKey, "Google Translate API key is required. Set ApiKey in GoogleTranslationOptions.");
            var url = $"{_baseUrl}/detect?key={Uri.EscapeDataString(apiKey)}";
            var requestBody = new { q = text };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
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

            var result = JsonSerializer.Deserialize<GoogleDetectLanguageResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
            if (ex != null)
                Metrics.RecordError(MetricNames[nameof(Translation.Constants.Metrics.DetectLanguageDuration)], ex);

            return LanguageCodeInfo.Unknown;
        }
    }

    /// <summary>Tests the connection to Google Translate.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    public override async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try {
            // Test by detecting language of a simple English word
            var result = await DetectLanguageAsync("hello", ct).ConfigureAwait(false);
            Logger.LogDebug("Successfully tested Google Translate connection");
            return result != LanguageCodeInfo.Unknown;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error testing Google Translate connection");
            return false;
        }
    }

    private class GoogleTranslateResponse
    {
        public GoogleTranslateData? Data { get; set; }
    }

    private class GoogleTranslateData
    {
        public List<GoogleTranslation>? Translations { get; set; }
    }

    private class GoogleTranslation
    {
        public string TranslatedText { get; } = string.Empty;

        public string? DetectedSourceLanguage { get; set; }
    }

    private class GoogleDetectLanguageResponse
    {
        public GoogleDetectLanguageData? Data { get; set; }
    }

    private class GoogleDetectLanguageData
    {
        public List<List<GoogleDetection>>? Detections { get; set; }
    }

    private class GoogleDetection
    {
        public string Language { get; } = string.Empty;

        public double Confidence { get; set; }
    }
}