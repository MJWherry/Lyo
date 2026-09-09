using Lyo.Typecast.Client.Models.Voices.Request;
using Lyo.Typecast.Client.Models.Voices.Response;

namespace Lyo.Typecast.Client;

/// <summary>Facade for voice operations.</summary>
public class VoiceManager
{
    private readonly TypecastClient _client;

    /// <summary>Constructs the VoiceManager class.</summary>
    /// <param name="client">Typecast client in use.</param>
    public VoiceManager(TypecastClient client) => _client = client;

    /// <summary>Returns all available voices with optional filters.</summary>
    /// <param name="request">Optional voice-list filters (model, gender, age, use_cases).</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Voices the API can use.</returns>
    public async Task<List<Voice>> ListVoicesAsync(VoiceListReq? request = null, CancellationToken ct = default)
    {
        var response = await _client.GetAsAsync<VoiceListReq, List<Voice>>("/v2/voices", request, ct: ct).ConfigureAwait(false);
        return response ?? [];
    }

    /// <summary>Loads details for a specific voice by voice ID.</summary>
    /// <param name="voiceId">Voice id such as "tc_60e5426de8b95f1d3000d7b5".</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <returns>Voice details, or null when missing.</returns>
    public async Task<Voice?> GetVoiceAsync(string voiceId, CancellationToken ct = default)
        => await _client.GetAsAsync<Voice>($"/v2/voices/{voiceId}", ct: ct).ConfigureAwait(false);
}