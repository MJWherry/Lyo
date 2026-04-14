using Lyo.Typecast.Client.Models.Voices.Request;
using Lyo.Typecast.Client.Models.Voices.Response;

namespace Lyo.Typecast.Client;

/// <summary>Manager for voice operations.</summary>
public class VoiceManager
{
    private readonly TypecastClient _client;

    /// <summary>Initializes a new instance of the VoiceManager class.</summary>
    /// <param name="client">The Typecast client instance.</param>
    public VoiceManager(TypecastClient client) => _client = client;

    /// <summary>Lists all available voices with optional filters.</summary>
    /// <param name="request">Optional filters for voice listing (model, gender, age, use_cases).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of available voices.</returns>
    public async Task<List<Voice>> ListVoicesAsync(VoiceListReq? request = null, CancellationToken ct = default)
    {
        var response = await _client.GetAsAsync<VoiceListReq, List<Voice>>("/v2/voices", request, ct: ct).ConfigureAwait(false);
        return response ?? [];
    }

    /// <summary>Gets details for a specific voice by voice ID.</summary>
    /// <param name="voiceId">The voice ID (e.g., "tc_60e5426de8b95f1d3000d7b5").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Voice details, or null if not found.</returns>
    public async Task<Voice?> GetVoiceAsync(string voiceId, CancellationToken ct = default)
        => await _client.GetAsAsync<Voice>($"/v2/voices/{voiceId}", ct: ct).ConfigureAwait(false);
}