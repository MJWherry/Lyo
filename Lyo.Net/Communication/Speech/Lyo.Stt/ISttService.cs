using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.Stt.Models;

namespace Lyo.Stt;

/// <summary>Service interface for converting speech to text.</summary>
public interface ISttService
{
    /// <summary>Recognizes speech from audio data.</summary>
    /// <param name="audioData">The audio data to transcribe.</param>
    /// <param name="languageCode">Optional language code. Uses default if not provided.</param>
    /// <param name="audioFormat">Optional audio format enum. Uses default if not provided.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the recognition operation containing transcribed text.</returns>
    Task<SttResult> RecognizeAsync(byte[] audioData, LanguageCodeInfo? languageCode = null, AudioFormat? audioFormat = null, CancellationToken ct = default);

    /// <summary>Recognizes speech from an audio file. The audio format is automatically detected from the file extension.</summary>
    /// <param name="audioFilePath">The path to the audio file to transcribe. The format is detected from the file extension.</param>
    /// <param name="languageCode">Optional language code. Uses default if not provided.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the recognition operation containing transcribed text.</returns>
    Task<SttResult> RecognizeFromFileAsync(string audioFilePath, LanguageCodeInfo? languageCode = null, CancellationToken ct = default);

    /// <summary>Recognizes speech from a stream.</summary>
    /// <param name="audioStream">The stream containing audio data to transcribe.</param>
    /// <param name="languageCode">Optional language code. Uses default if not provided.</param>
    /// <param name="audioFormat">Optional audio format enum. Uses default if not provided.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the recognition operation containing transcribed text.</returns>
    Task<SttResult> RecognizeFromStreamAsync(Stream audioStream, LanguageCodeInfo? languageCode = null, AudioFormat? audioFormat = null, CancellationToken ct = default);

    /// <summary>Recognizes speech using a request object.</summary>
    /// <param name="request">The STT request containing audio data and options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the recognition operation containing transcribed text.</returns>
    Task<SttResult> RecognizeAsync(SttRequest request, CancellationToken ct = default);

    /// <summary>Recognizes multiple audio inputs in bulk.</summary>
    /// <param name="requests">Collection of STT requests.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of results for each recognition operation.</returns>
    Task<IReadOnlyList<SttResult>> RecognizeBulkAsync(IEnumerable<SttRequest> requests, CancellationToken ct = default);

    /// <summary>Tests the connection to the STT service provider.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}