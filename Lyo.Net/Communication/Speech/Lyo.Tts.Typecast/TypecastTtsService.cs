using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Tts.Models;
using Lyo.Typecast.Client;
using Lyo.Typecast.Client.Models.TextToSpeech.Request;
using Lyo.Typecast.Client.Models.Voices.Response;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Tts.Typecast;

/// <summary>Typecast TTS service implementation using the Typecast API for converting text to speech.</summary>
/// <remarks>
/// <para>
/// This class is thread-safe. All instance fields are readonly and operations use thread-safe collections and synchronization primitives from the base class for bulk
/// operations.
/// </para>
/// </remarks>
public sealed class TypecastTtsService : TtsServiceBase<TypecastTtsRequest>
{
    /// <summary>The default voice ID to use when none is specified. This is a commonly available English voice.</summary>
    private const string DefaultVoiceId = "tc_689450bdcce4027c2f06eee8";

    private readonly SemaphoreSlim _loadVoicesSemaphore;
    private readonly TypecastOptions _options;
    private readonly TypecastClient _typecastClient;
    private readonly ConcurrentDictionary<(string model, string voiceId), Voice> _voicesByModel;
    private bool _voicesLoaded;

    /// <summary>Initializes a new instance of the TypecastTtsService class.</summary>
    /// <param name="typecastClient">The Typecast client. Must not be null.</param>
    /// <param name="options">The Typecast configuration options. Must not be null.</param>
    /// <param name="logger">Optional logger instance for logging operations.</param>
    /// <param name="metrics">Optional metrics instance for tracking TTS operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when typecastClient or options is null.</exception>
    public TypecastTtsService(TypecastClient typecastClient, TypecastOptions options, ILogger<TypecastTtsService>? logger = null, IMetrics? metrics = null)
        : base(options, logger ?? NullLoggerFactory.Instance.CreateLogger<TypecastTtsService>(), metrics)
    {
        _typecastClient = typecastClient;
        _options = options;
        _voicesByModel = new(new InlineVoiceKeyComparer());
        _loadVoicesSemaphore = new(1, 1);
        _voicesLoaded = false;

        // Override base metric names with Typecast-specific ones
        MetricNames[nameof(Tts.Constants.Metrics.SynthesizeDuration)] = Constants.Metrics.SynthesizeDuration;
        MetricNames[nameof(Tts.Constants.Metrics.SynthesizeSuccess)] = Constants.Metrics.SynthesizeSuccess;
        MetricNames[nameof(Tts.Constants.Metrics.SynthesizeFailure)] = Constants.Metrics.SynthesizeFailure;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeDuration)] = Constants.Metrics.BulkSynthesizeDuration;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeTotal)] = Constants.Metrics.BulkSynthesizeTotal;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeSuccess)] = Constants.Metrics.BulkSynthesizeSuccess;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeFailure)] = Constants.Metrics.BulkSynthesizeFailure;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeLastDurationMs)] = Constants.Metrics.BulkSynthesizeLastDurationMs;
    }

    /// <summary>Releases the unmanaged resources used by the TypecastTtsService and optionally releases the managed resources.</summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing) {
            _loadVoicesSemaphore.Dispose();
            _typecastClient.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Loads available voices from the Typecast API and organizes them by model and voice ID.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if voices were loaded successfully, false otherwise.</returns>
    /// <remarks>
    /// <para>This method is thread-safe and will only load voices once, even if called multiple times concurrently.</para>
    /// <para>Voices are stored using a composite key of (model, voiceId) with the full Voice object as the value.</para>
    /// </remarks>
    public async Task<bool> LoadVoicesAsync(CancellationToken ct = default)
    {
        // Ensure only one thread loads voices at a time
        await _loadVoicesSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try {
            // Check if already loaded
            if (_voicesLoaded) {
                Logger.LogDebug("Voices already loaded, skipping reload");
                return true;
            }

            Logger.LogInformation("Loading Typecast voices from API");
            var voices = await _typecastClient.Voices.ListVoicesAsync(ct: ct).ConfigureAwait(false);
            if (voices.Count == 0) {
                Logger.LogWarning("No voices returned from Typecast API");
                return false;
            }

            // Clear existing data
            _voicesByModel.Clear();

            // Store voices by (model, voiceId) composite key
            foreach (var voice in voices) {
                if (voice.Models.Count == 0)
                    continue;

                foreach (var model in voice.Models) {
                    if (string.IsNullOrWhiteSpace(model.Version))
                        continue;

                    var modelKey = model.Version.ToLowerInvariant();
                    var key = (model: modelKey, voiceId: voice.VoiceId.ToLowerInvariant());
                    _voicesByModel.TryAdd(key, voice);
                }
            }

            _voicesLoaded = true;
            var uniqueModels = _voicesByModel.Keys.Select(k => k.model).Distinct().Count();
            Logger.LogInformation("Successfully loaded {VoiceCount} voices across {ModelCount} models", voices.Count, uniqueModels);

            // Log summary by model
            var voicesByModelGrouped = _voicesByModel.GroupBy(kvp => kvp.Key.model).OrderBy(g => g.Key);
            foreach (var group in voicesByModelGrouped)
                Logger.LogDebug("Model {Model}: {Count} voices available", group.Key, group.Count());

            return true;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error loading voices from Typecast API");
            return false;
        }
        finally {
            _loadVoicesSemaphore.Release();
        }
    }

    /// <summary>Gets the available voices for a specific model.</summary>
    /// <param name="model">The model version (e.g., "ssfm-v30").</param>
    /// <returns>A read-only list of Voice objects that support the specified model, or an empty list if the model is not found or voices haven't been loaded.</returns>
    public IReadOnlyList<Voice> GetVoicesForModel(string model)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(model);
        var modelKey = model.ToLowerInvariant();
        var voices = _voicesByModel.Where(kvp => kvp.Key.model == modelKey).Select(kvp => kvp.Value).ToList();
        return voices.AsReadOnly();
    }

    /// <summary>Validates that a voice ID is available for the specified model.</summary>
    /// <param name="voiceId">The voice ID to validate.</param>
    /// <param name="model">The model version.</param>
    /// <returns>True if the voice is available for the model, false otherwise.</returns>
    private bool IsVoiceAvailableForModel(string voiceId, string model)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(voiceId);
        ArgumentHelpers.ThrowIfNullOrEmpty(model);
        var key = (model: model.ToLowerInvariant(), voiceId: voiceId.ToLowerInvariant());
        return _voicesByModel.ContainsKey(key);
    }

    /// <summary>Synthesizes text to speech and saves it to a file using a builder.</summary>
    /// <param name="builder">The TTS request builder.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A TtsResult indicating success or failure.</returns>
    public async Task<TtsResult<TypecastTtsRequest>> SynthesizeToFileAsync(TypecastTtsRequestBuilder builder, CancellationToken ct = default)
        => await SynthesizeAsync(builder.Build(), ct).ConfigureAwait(false);

    /// <summary>Synthesizes text to speech using the Typecast API.</summary>
    /// <param name="request">The TTS request containing text and options.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A TtsResult indicating success or failure with audio data.</returns>
    protected override async Task<TtsResult<TypecastTtsRequest>> SynthesizeCoreAsync(TypecastTtsRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.Text, nameof(request.Text));
            ArgumentHelpers.ThrowIfNotInRange(request.Text.Length, 1, Options.MaxTextLength, nameof(request.Text));
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Error validating Typecast TTS request");
            return TtsResult<TypecastTtsRequest>.FromException(ex, request, sw.Elapsed, TtsErrorCodes.SynthesizeFailed);
        }

        request.VoiceId ??= _options.DefaultVoiceId ?? DefaultVoiceId;
        if (_voicesLoaded) {
            var model = request.Model ?? _options.DefaultModel;
            if (!IsVoiceAvailableForModel(request.VoiceId, model)) {
                sw.Stop();
                var availableVoices = GetVoicesForModel(model);
                var errorMessage = $"Voice '{request.VoiceId}' is not available for model '{model}'. " + (availableVoices.Count > 0
                    ? $"Available voices for this model: {string.Join(", ", availableVoices.Select(v => v.VoiceId).Take(10))}{(availableVoices.Count > 10 ? "..." : "")}"
                    : "No voices have been loaded for this model.");

                Logger.LogWarning(errorMessage);
                return TtsResult<TypecastTtsRequest>.FromException(new ArgumentException(errorMessage), request, sw.Elapsed, TtsErrorCodes.SynthesizeFailed);
            }
        }
        else
            Logger.LogDebug("Voices not loaded, skipping voice validation for VoiceId: {VoiceId}, Model: {Model}", request.VoiceId, request.Model);

        try {
            Logger.LogDebug("Typecast TTS request - VoiceId: {VoiceId}, TextLength: {TextLength}, Model: {Model}", request.VoiceId, request.Text.Length, request.Model);
            var audioData = await _typecastClient.TextToSpeech.SynthesizeAsync(request, ct).ConfigureAwait(false);
            sw.Stop();
            if (audioData.Length == 0) {
                Logger.LogWarning("Typecast API returned empty audio data");
                return TtsResult<TypecastTtsRequest>.FromException(new InvalidOperationException("Empty audio data received"), request, sw.Elapsed, TtsErrorCodes.SynthesizeFailed);
            }

            Logger.LogDebug("Successfully synthesized text to speech using Typecast API. VoiceId: {VoiceId}, Length: {Length} bytes", request.VoiceId, audioData.Length);
            return TtsResult<TypecastTtsRequest>.FromSuccess(request, audioData, sw.Elapsed, null, "Speech synthesized successfully");
        }
        catch (OperationCanceledException) {
            sw.Stop();
            Logger.LogWarning("Typecast TTS synthesis was cancelled");
            return TtsResult<TypecastTtsRequest>.FromException(new OperationCanceledException(ct), request, sw.Elapsed, TtsErrorCodes.OperationCancelled);
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Error synthesizing text to speech with Typecast API");
            return TtsResult<TypecastTtsRequest>.FromException(ex, request, sw.Elapsed, TtsErrorCodes.SynthesizeFailed);
        }
    }

    /// <summary>Synthesizes text to speech using the Typecast API.</summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="voiceId">Optional voice ID. If not provided, uses the default voice from options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A TtsResult indicating success or failure with audio data.</returns>
    public override async Task<TtsResult<TypecastTtsRequest>> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default)
    {
        var finalVoiceId = voiceId ?? _options.DefaultVoiceId ?? DefaultVoiceId;
        var request = TypecastTtsRequestBuilder.Create(finalVoiceId, text).WithModel(_options.DefaultModel).Build();
        return await SynthesizeAsync(request, ct).ConfigureAwait(false);
    }

    /// <summary>Tests the connection to the Typecast API.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    public override async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try {
            // Test connection by getting available voices using the client
            Logger.LogDebug("Testing Typecast API connection");
            var voices = await _typecastClient.Voices.ListVoicesAsync(ct: ct).ConfigureAwait(false);
            if (voices.Count > 0) {
                Logger.LogDebug("Successfully tested Typecast API connection. Found {Count} voices", voices.Count);
                return true;
            }

            Logger.LogWarning("Typecast API connection test returned no voices");
            return false;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error testing Typecast connection");
            return false;
        }
    }

    /// <summary>Inline comparer for (model, voiceId) tuple keys with case-insensitive comparison.</summary>
    private sealed class InlineVoiceKeyComparer : IEqualityComparer<(string model, string voiceId)>
    {
        public bool Equals((string model, string voiceId) x, (string model, string voiceId) y)
            => string.Equals(x.model, y.model, StringComparison.OrdinalIgnoreCase) && string.Equals(x.voiceId, y.voiceId, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string model, string voiceId) obj)
        {
            unchecked {
                var hash = 17;
                hash = hash * 23 + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.model);
                hash = hash * 23 + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.voiceId);
                return hash;
            }
        }
    }
}