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

/// <summary>Typecast API-backed TTS that turns text into speech.</summary>
/// <remarks>
/// <para>
/// Thread-safe: instance fields are readonly, and bulk work uses the base class's thread-safe collections and synchronization
/// primitives.
/// </para>
/// </remarks>
public sealed class TypecastTtsService : TtsServiceBase<TypecastTtsRequest>
{
    /// <summary>Voice used when the caller omits one. A commonly available English voice.</summary>
    private const string DefaultVoiceId = "tc_689450bdcce4027c2f06eee8";

    private readonly SemaphoreSlim _loadVoicesSemaphore;
    private readonly TypecastOptions _options;
    private readonly TypecastClient _typecastClient;
    private readonly ConcurrentDictionary<(string model, string voiceId), Voice> _voicesByModel;
    private bool _voicesLoaded;

    /// <summary>Constructs a TypecastTtsService.</summary>
    /// <param name="typecastClient">Typecast HTTP client. Must not be null.</param>
    /// <param name="options">Typecast settings. Must not be null.</param>
    /// <param name="logger">Optional logger for service activity.</param>
    /// <param name="metrics">Optional metrics sink for TTS work.</param>
    /// <exception cref="ArgumentNullException">Thrown when typecastClient or options is null.</exception>
    public TypecastTtsService(TypecastClient typecastClient, TypecastOptions options, ILogger<TypecastTtsService>? logger = null, IMetrics? metrics = null)
        : base(options, logger ?? NullLoggerFactory.Instance.CreateLogger<TypecastTtsService>(), metrics)
    {
        _typecastClient = typecastClient;
        _options = options;
        _voicesByModel = new(new InlineVoiceKeyComparer());
        _loadVoicesSemaphore = new(1, 1);
        _voicesLoaded = false;

        // Replace base metric names with the Typecast set
        MetricNames[nameof(Tts.Constants.Metrics.SynthesizeDuration)] = Constants.Metrics.SynthesizeDuration;
        MetricNames[nameof(Tts.Constants.Metrics.SynthesizeSuccess)] = Constants.Metrics.SynthesizeSuccess;
        MetricNames[nameof(Tts.Constants.Metrics.SynthesizeFailure)] = Constants.Metrics.SynthesizeFailure;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeDuration)] = Constants.Metrics.BulkSynthesizeDuration;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeTotal)] = Constants.Metrics.BulkSynthesizeTotal;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeSuccess)] = Constants.Metrics.BulkSynthesizeSuccess;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeFailure)] = Constants.Metrics.BulkSynthesizeFailure;
        MetricNames[nameof(Tts.Constants.Metrics.BulkSynthesizeLastDurationMs)] = Constants.Metrics.BulkSynthesizeLastDurationMs;
    }

    /// <summary>Frees unmanaged resources, and managed ones when <paramref name="disposing" /> is true.</summary>
    /// <param name="disposing">True to free managed and unmanaged resources; false for unmanaged only.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing) {
            _loadVoicesSemaphore.Dispose();
            _typecastClient.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Fetches Typecast voices and indexes them by model and voice ID.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when voices loaded; otherwise false.</returns>
    /// <remarks>
    /// <para>Thread-safe: concurrent callers load voices only once.</para>
    /// <para>Each entry is keyed by (model, voiceId) and stores the full Voice object.</para>
    /// </remarks>
    public async Task<bool> LoadVoicesAsync(CancellationToken ct = default)
    {
        // Serialize voice loading so only one thread fetches
        await _loadVoicesSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try {
            // Skip when voices are already cached
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

            // Drop any prior cache
            _voicesByModel.Clear();

            // Index each voice under (model, voiceId)
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

            // Write a per-model summary
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

    /// <summary>Voices that support a given model.</summary>
    /// <param name="model">Model version (for example "ssfm-v30").</param>
    /// <returns>Read-only Voice list for that model, or empty if the model is unknown or voices are not loaded.</returns>
    public IReadOnlyList<Voice> GetVoicesForModel(string model)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(model);
        var modelKey = model.ToLowerInvariant();
        var voices = _voicesByModel.Where(kvp => kvp.Key.model == modelKey).Select(kvp => kvp.Value).ToList();
        return voices.AsReadOnly();
    }

    /// <summary>Checks whether a voice exists for the given model.</summary>
    /// <param name="voiceId">Voice ID to check.</param>
    /// <param name="model">Model version.</param>
    /// <returns>True when that voice is available for the model; otherwise false.</returns>
    private bool IsVoiceAvailableForModel(string voiceId, string model)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(voiceId);
        ArgumentHelpers.ThrowIfNullOrEmpty(model);
        var key = (model: model.ToLowerInvariant(), voiceId: voiceId.ToLowerInvariant());
        return _voicesByModel.ContainsKey(key);
    }

    /// <summary>Builds a <see cref="TypecastTtsRequest" /> from <paramref name="builder" /> and returns audio in the result (no disk I/O).</summary>
    /// <remarks>To write a file, use the base class file overload with <c>builder.Build()</c> instead of this method.</remarks>
    /// <param name="builder">Fluent request builder.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    public async Task<TtsResult<TypecastTtsRequest>> SynthesizeToFileAsync(TypecastTtsRequestBuilder builder, CancellationToken ct = default)
        => await SynthesizeAsync(builder.Build(), ct).ConfigureAwait(false);

    /// <summary>Speaks via the Typecast API from a request object.</summary>
    /// <param name="request">Request holding the text and options.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>TtsResult with success or failure and audio bytes when successful.</returns>
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

    /// <summary>Speaks text through the Typecast API.</summary>
    /// <param name="text">Text to synthesize.</param>
    /// <param name="voiceId">Optional voice. When omitted, uses the options default.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>TtsResult with success or failure and audio bytes when successful.</returns>
    public override async Task<TtsResult<TypecastTtsRequest>> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default)
    {
        var finalVoiceId = voiceId ?? _options.DefaultVoiceId ?? DefaultVoiceId;
        var request = TypecastTtsRequestBuilder.Create(finalVoiceId, text).WithModel(_options.DefaultModel).Build();
        return await SynthesizeAsync(request, ct).ConfigureAwait(false);
    }

    /// <summary>Checks that the Typecast API can be reached.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the API answers; otherwise false.</returns>
    public override async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try {
            // Probe the API by listing voices through the client
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

    /// <summary>Case-insensitive equality for (model, voiceId) tuple keys.</summary>
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