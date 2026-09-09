using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Stt.Models;

namespace Lyo.Stt;

/// <summary>Contract for turning speech into text.</summary>
public interface ISttService
{
    /// <summary>Transcribes speech from audio bytes.</summary>
    /// <param name="audioData">Audio to transcribe.</param>
    /// <param name="languageCode">Optional language; the default is used when omitted.</param>
    /// <param name="audioFormat">Optional format enum; the default is used when omitted.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Outcome that includes the transcript when successful.</returns>
    Task<SttResult> RecognizeAsync(byte[] audioData, LanguageCodeInfo? languageCode = null, AudioFormat? audioFormat = null, CancellationToken ct = default);

    /// <summary>Transcribes speech from a file. Format is inferred from the extension.</summary>
    /// <param name="audioFilePath">Path of the audio file. Format is taken from the extension.</param>
    /// <param name="languageCode">Optional language; the default is used when omitted.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Outcome that includes the transcript when successful.</returns>
    Task<SttResult> RecognizeFromFileAsync(string audioFilePath, LanguageCodeInfo? languageCode = null, CancellationToken ct = default);

    /// <summary>Transcribes speech from a stream.</summary>
    /// <param name="audioStream">Stream of audio bytes.</param>
    /// <param name="languageCode">Optional language; the default is used when omitted.</param>
    /// <param name="audioFormat">Optional format enum; the default is used when omitted.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Outcome that includes the transcript when successful.</returns>
    Task<SttResult> RecognizeFromStreamAsync(Stream audioStream, LanguageCodeInfo? languageCode = null, AudioFormat? audioFormat = null, CancellationToken ct = default);

    /// <summary>Transcribes using a populated request.</summary>
    /// <param name="request">Request holding audio and options.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>Outcome that includes the transcript when successful.</returns>
    Task<SttResult> RecognizeAsync(SttRequest request, CancellationToken ct = default);

    /// <summary>Transcribes many audio inputs in one bulk call.</summary>
    /// <param name="requests">Requests to process together.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>One result per request.</returns>
    Task<IReadOnlyList<SttResult>> RecognizeBulkAsync(IEnumerable<SttRequest> requests, CancellationToken ct = default);

    /// <summary>Checks that the STT provider can be reached.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the provider answers; otherwise false.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}