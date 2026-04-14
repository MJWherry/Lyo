using Lyo.Tts.Models;

namespace Lyo.Tts;

/// <summary>Non-generic TTS for hosts that register one provider (e.g. Discord bot). Coexists with <see cref="ITtsService{TRequest}" />.</summary>
public interface ITtsService
{
    Task<TtsSynthesisResult> SynthesizeAsync(string text, string? voiceId = null, CancellationToken cancellationToken = default);
}

/// <summary>Service interface for converting text to speech.</summary>
public interface ITtsService<TRequest>
    where TRequest : TtsRequest
{
    Task<TtsResult<TRequest>> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default);

    Task<TtsResult<TRequest>> SynthesizeAsync(TRequest request, CancellationToken ct = default);

    Task<TtsResult<TRequest>> SynthesizeToFileAsync(string text, string outputFilePath, string? voiceId = null, CancellationToken ct = default);

    Task<TtsResult<TRequest>> SynthesizeToFileAsync(TRequest request, string outputFilePath, CancellationToken ct = default);

    Task<TtsResult<TRequest>> SynthesizeToStreamAsync(string text, Stream outputStream, string? voiceId = null, CancellationToken ct = default);

    Task<TtsResult<TRequest>> SynthesizeToStreamAsync(TRequest text, Stream outputStream, CancellationToken ct = default);

    Task<IReadOnlyList<TtsResult<TRequest>>> SynthesizeBulkAsync(IEnumerable<TRequest> requests, CancellationToken ct = default);

    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}