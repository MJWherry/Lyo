using Lyo.Tts.Models;

namespace Lyo.Tts;

/// <summary>Non-generic TTS contract for scenarios that register a single backend (simple hosts, façade types). Works alongside <see cref="ITtsService{TRequest}" />.</summary>
public interface ITtsService
{
    /// <summary>Synthesizes speech from plain text.</summary>
    /// <param name="text">The text to speak.</param>
    /// <param name="voiceId">Optional voice identifier understood by the implementation (often an enum name or provider voice id).</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A compact success or failure outcome with optional audio bytes.</returns>
    Task<TtsSynthesisResult> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default);
}

/// <summary>Typed TTS contract: synthesize via string overloads or full <typeparamref name="TRequest" /> objects, write to disk or streams, bulk calls, and health checks.</summary>
/// <typeparam name="TRequest">Concrete request type for the provider (e.g. Polly-specific options).</typeparam>
public interface ITtsService<TRequest>
    where TRequest : TtsRequest
{
    /// <summary>Synthesizes speech from <paramref name="text" /> using defaults from options plus an optional voice override.</summary>
    Task<TtsResult<TRequest>> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default);

    /// <summary>Synthesizes speech using a fully populated <paramref name="request" />.</summary>
    Task<TtsResult<TRequest>> SynthesizeAsync(TRequest request, CancellationToken ct = default);

    /// <summary>Synthesizes speech and writes the returned audio bytes to <paramref name="outputFilePath" />.</summary>
    Task<TtsResult<TRequest>> SynthesizeToFileAsync(string text, string outputFilePath, string? voiceId = null, CancellationToken ct = default);

    /// <summary>Synthesizes speech from <paramref name="request" /> and writes audio to <paramref name="outputFilePath" />.</summary>
    Task<TtsResult<TRequest>> SynthesizeToFileAsync(TRequest request, string outputFilePath, CancellationToken ct = default);

    /// <summary>Synthesizes speech and appends audio bytes to <paramref name="outputStream" /> (stream must be writable).</summary>
    Task<TtsResult<TRequest>> SynthesizeToStreamAsync(string text, Stream outputStream, string? voiceId = null, CancellationToken ct = default);

    /// <summary>Synthesizes speech from <paramref name="request" /> and writes audio to <paramref name="outputStream" />.</summary>
    Task<TtsResult<TRequest>> SynthesizeToStreamAsync(TRequest request, Stream outputStream, CancellationToken ct = default);

    /// <summary>Runs synthesis for each request with concurrency capped by service options.</summary>
    Task<IReadOnlyList<TtsResult<TRequest>>> SynthesizeBulkAsync(IEnumerable<TRequest> requests, CancellationToken ct = default);

    /// <summary>Returns whether the backend accepts a lightweight API call (provider-defined).</summary>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}