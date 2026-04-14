using Lyo.Typecast.Client.Models.TextToSpeech.Request;

namespace Lyo.Typecast.Client;

/// <summary>Manager for text-to-speech operations.</summary>
public class TextToSpeechManager
{
    private readonly TypecastClient _client;

    /// <summary>Initializes a new instance of the TextToSpeechManager class.</summary>
    /// <param name="client">The Typecast client instance.</param>
    public TextToSpeechManager(TypecastClient client) => _client = client;

    /// <summary>Synthesizes text to speech using the Typecast API.</summary>
    /// <param name="request">The text-to-speech request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Audio data as byte array (WAV or MP3 format).</returns>
    public async Task<byte[]> SynthesizeAsync(TypecastTtsRequest request, CancellationToken ct = default)
        => await _client.PostAsBinaryAsync("/v1/text-to-speech", request, null, ct).ConfigureAwait(false);
}