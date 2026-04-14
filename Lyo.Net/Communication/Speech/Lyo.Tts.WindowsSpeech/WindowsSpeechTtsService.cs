#if WINDOWS || NETFRAMEWORK
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Tts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#if NETSTANDARD2_0
using OSPlatform = System.Runtime.InteropServices.OSPlatform;
#endif

namespace Lyo.Tts.WindowsSpeech;

/// <summary>Windows Speech Synthesis TTS service implementation using System.Speech.Synthesis for converting text to speech.</summary>
/// <remarks>
/// <para>
/// This class uses the Windows built-in Speech Synthesis API (SAPI) and is only available on Windows platforms.
/// </para>
/// <para>
/// This class is thread-safe. All instance fields are readonly and operations use thread-safe collections and synchronization primitives from the base class for bulk
/// operations.
/// </para>
/// </remarks>
public sealed class WindowsSpeechTtsService : TtsServiceBase<WindowsTtsRequest>
{
    private readonly SpeechSynthesizer _synthesizer;

    /// <summary>Initializes a new instance of the WindowsSpeechTtsService class.</summary>
    /// <param name="options">The TTS service options. Must not be null.</param>
    /// <param name="logger">Optional logger instance for logging operations.</param>
    /// <param name="metrics">Optional metrics instance for tracking TTS operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when not running on Windows.</exception>
    public WindowsSpeechTtsService(TtsServiceOptions options, ILogger<WindowsSpeechTtsService>? logger = null, IMetrics? metrics = null)
        : base(options, logger ?? NullLoggerFactory.Instance.CreateLogger<WindowsSpeechTtsService>(), metrics)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("WindowsSpeechTtsService requires Windows platform.");

        _synthesizer = new SpeechSynthesizer();
        
        // Override base metric names with Windows Speech-specific ones
        MetricNames[nameof(Lyo.Tts.Constants.Metrics.SynthesizeDuration)] = Constants.Metrics.SynthesizeDuration;
        MetricNames[nameof(Lyo.Tts.Constants.Metrics.SynthesizeSuccess)] = Constants.Metrics.SynthesizeSuccess;
        MetricNames[nameof(Lyo.Tts.Constants.Metrics.SynthesizeFailure)] = Constants.Metrics.SynthesizeFailure;
        MetricNames[nameof(Lyo.Tts.Constants.Metrics.BulkSynthesizeDuration)] = Constants.Metrics.BulkSynthesizeDuration;
        MetricNames[nameof(Lyo.Tts.Constants.Metrics.BulkSynthesizeTotal)] = Constants.Metrics.BulkSynthesizeTotal;
        MetricNames[nameof(Lyo.Tts.Constants.Metrics.BulkSynthesizeSuccess)] = Constants.Metrics.BulkSynthesizeSuccess;
        MetricNames[nameof(Lyo.Tts.Constants.Metrics.BulkSynthesizeFailure)] = Constants.Metrics.BulkSynthesizeFailure;
        MetricNames[nameof(Lyo.Tts.Constants.Metrics.BulkSynthesizeLastDurationMs)] = Constants.Metrics.BulkSynthesizeLastDurationMs;
    }

    /// <summary>Releases the unmanaged resources used by the WindowsSpeechTtsService and optionally releases the managed resources.</summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _synthesizer != null) {
            _synthesizer.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Synthesizes text to speech using Windows Speech Synthesis.</summary>
    /// <param name="request">The TTS request containing text and options.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A TtsResult indicating success or failure with audio data.</returns>
    protected override async Task<TtsResult<WindowsTtsRequest>> SynthesizeCoreAsync(WindowsTtsRequest request, CancellationToken ct = default)
{
    var sw = Stopwatch.StartNew();
    try {
        ArgumentHelpers.ThrowIfNotInRange(request.Text.Length, 1, Options.MaxTextLength, nameof(request.Text));
    }
    catch (Exception ex) {
        sw.Stop();
        Logger.LogError(ex, "Error validating Windows TTS request");
        return TtsResult<WindowsTtsRequest>.FromException(ex, request, sw.Elapsed, TtsErrorCodes.SynthesizeFailed);
    }

    try {
        using var synthesizer = new SpeechSynthesizer();

        if (!string.IsNullOrWhiteSpace(request.VoiceId)) {
            try { synthesizer.SelectVoice(request.VoiceId); }
            catch (Exception ex) { Logger.LogWarning(ex, "Failed to select voice {VoiceId}, using default", request.VoiceId); }
        }

        if (int.TryParse(request.SpeechRate, out var rate))
            synthesizer.Rate =Clamp(rate, -10, 10);

        if (int.TryParse(request.Volume, out var volume))
            synthesizer.Volume = Clamp(volume, 0, 100);

        byte[] audioData;
        using (var memoryStream = new MemoryStream()) {
            synthesizer.SetOutputToWaveStream(memoryStream);
            
            var tcs = new TaskCompletionSource<bool>();
            synthesizer.SpeakCompleted += (sender, args) => {
                if (args.Error != null) tcs.TrySetException(args.Error);
                else if (args.Cancelled) tcs.TrySetCanceled();
                else tcs.TrySetResult(true);
            };

            using (ct.Register(() => {
                synthesizer.SpeakAsyncCancelAll();
                tcs.TrySetCanceled();
            })) {
                synthesizer.SpeakAsync(request.Text);
                await tcs.Task;
            }

            audioData = memoryStream.ToArray();
        }

        sw.Stop();

        if (audioData.Length == 0) {
            Logger.LogWarning("Windows Speech Synthesis returned empty audio data");
            return TtsResult<WindowsTtsRequest>.FromException(new InvalidOperationException("Empty audio data received"), request, sw.Elapsed, TtsErrorCodes.SynthesizeFailed);
        }

        Logger.LogDebug("Successfully synthesized {Length} bytes using Windows Speech Synthesis", audioData.Length);
        return TtsResult<WindowsTtsRequest>.FromSuccess(request, audioData, sw.Elapsed, null, "Speech synthesized successfully");
    }
    catch (OperationCanceledException) {
        sw.Stop();
        Logger.LogWarning("Windows Speech Synthesis was cancelled");
        return TtsResult<WindowsTtsRequest>.FromException(new OperationCanceledException(ct), request, sw.Elapsed, TtsErrorCodes.OperationCancelled);
    }
    catch (Exception ex) {
        sw.Stop();
        Logger.LogError(ex, "Error synthesizing text to speech with Windows Speech Synthesis");
        return TtsResult<WindowsTtsRequest>.FromException(ex, request, sw.Elapsed, TtsErrorCodes.SynthesizeFailed);
    }
}

    public override Task<TtsResult<WindowsTtsRequest>> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default)
        => SynthesizeCoreAsync(new WindowsTtsRequest(){Text = text, VoiceId = voiceId}, ct);

    /// <summary>Tests the connection to Windows Speech Synthesis.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    public override async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try {
            // Test by getting available voices
            var voices = await Task.Run(() => _synthesizer.GetInstalledVoices(), ct);
            var hasVoices = voices.Count > 0;
            Logger.LogDebug("Windows Speech Synthesis test: {VoiceCount} voices available", voices.Count);
            return hasVoices;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error testing Windows Speech Synthesis connection");
            return false;
        }
    }

    private static T Clamp<T>(T value, T min, T max) where T : System.IComparable<T> 
        => value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
}
#endif

