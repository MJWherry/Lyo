using Lyo.Tts.Models;

namespace Lyo.Tts;

/// <summary>Non-generic TTS contract for hosts that register one backend (simple hosts, façades). Used next to <see cref="ITtsService{TRequest}" />.</summary>
public interface ITtsService
{
    /// <summary>Speaks plain text.</summary>
    /// <param name="text">Text to speak.</param>
    /// <param name="voiceId">Optional voice id the implementation understands (often an enum name or provider voice id).</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>A compact success or failure with optional audio bytes.</returns>
    Task<TtsSynthesisResult> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default);
}

/// <summary>Typed TTS contract: synthesize from strings or full <typeparamref name="TRequest" /> objects, write to disk or streams, run bulk calls, and probe health.</summary>
/// <typeparam name="TRequest">Provider request type (for example Polly-specific options).</typeparam>
public interface ITtsService<TRequest>
    where TRequest : TtsRequest
{
    /// <summary>Speaks <paramref name="text" /> using option defaults and an optional voice override.</summary>
    Task<TtsResult<TRequest>> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default);

    /// <summary>Speaks from a fully populated <paramref name="request" />.</summary>
    Task<TtsResult<TRequest>> SynthesizeAsync(TRequest request, CancellationToken ct = default);

    /// <summary>Speaks and writes the audio bytes to <paramref name="outputFilePath" />.</summary>
    Task<TtsResult<TRequest>> SynthesizeToFileAsync(string text, string outputFilePath, string? voiceId = null, CancellationToken ct = default);

    /// <summary>Speaks <paramref name="request" /> and writes audio to <paramref name="outputFilePath" />.</summary>
    Task<TtsResult<TRequest>> SynthesizeToFileAsync(TRequest request, string outputFilePath, CancellationToken ct = default);

    /// <summary>Speaks and appends audio to <paramref name="outputStream" /> (must be writable).</summary>
    Task<TtsResult<TRequest>> SynthesizeToStreamAsync(string text, Stream outputStream, string? voiceId = null, CancellationToken ct = default);

    /// <summary>Speaks <paramref name="request" /> and writes audio to <paramref name="outputStream" />.</summary>
    Task<TtsResult<TRequest>> SynthesizeToStreamAsync(TRequest request, Stream outputStream, CancellationToken ct = default);

    /// <summary>Synthesizes each request, limited by the service concurrency cap.</summary>
    Task<IReadOnlyList<TtsResult<TRequest>>> SynthesizeBulkAsync(IEnumerable<TRequest> requests, CancellationToken ct = default);

    /// <summary>Whether the backend answers a lightweight probe (provider-defined).</summary>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}