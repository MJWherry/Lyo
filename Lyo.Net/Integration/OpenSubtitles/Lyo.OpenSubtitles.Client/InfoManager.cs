using Lyo.Exceptions;

namespace Lyo.OpenSubtitles.Client;

/// <summary>Authenticated account info.</summary>
public sealed class InfoManager(OpenSubtitlesClient client)
{
    /// <summary>Reads the logged-in user's quota. Sends the session bearer to <c>GET /infos/user</c> on the login hostname.</summary>
    public async Task<OpenSubtitlesUser> GetUserAsync(OpenSubtitlesSession session, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(session);
        var envelope = await client.GetAsAsync<UserEnvelope>(client.Url(session, "infos/user"), OpenSubtitlesClient.Bearer(session), ct).ConfigureAwait(false);
        if (envelope?.Data == null)
            throw new InvalidOperationException("OpenSubtitles user response did not include data.");

        return envelope.Data;
    }

    internal sealed class UserEnvelope
    {
        public OpenSubtitlesUser? Data { get; set; }
    }
}