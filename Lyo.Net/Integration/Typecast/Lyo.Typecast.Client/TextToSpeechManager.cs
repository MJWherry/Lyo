using Lyo.Typecast.Client.Models.TextToSpeech.Request;

namespace Lyo.Typecast.Client;

/// <summary>Facade for text-to-speech operations.</summary>
public class TextToSpeechManager
{
    private readonly TypecastClient _client;

    /// <summary>Constructs the TextToSpeechManager class.</summary>
    /// <param name="client">Typecast client in use.</param>
    public TextToSpeechManager(TypecastClient client) => _client = client;

    /// <summary>Turns text into speech through the Typecast API.</summary>
    /// <param name="request">TTS request payload.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Audio bytes in WAV or MP3.</returns>
    public async Task<byte[]> SynthesizeAsync(TypecastTtsRequest request, CancellationToken ct = default)
        => await _client.PostAsBinaryAsync("/v1/text-to-speech", request, null, ct).ConfigureAwait(false);
}