using Lyo.Exceptions;

namespace Lyo.OpenSubtitles.Client;

/// <summary>Login result. Pass it to calls that must stay on the returned host or send the user token.</summary>
public sealed class OpenSubtitlesSession
{
    /// <summary>JWT from <c>POST /login</c>.</summary>
    public string Token { get; }

    /// <summary>Hostname for later calls in this session. No scheme and no path.</summary>
    public string Host { get; }

    /// <summary>Creates a session from a login <paramref name="token" /> and bare <paramref name="host" /> such as <c>vip-api.opensubtitles.com</c>.</summary>
    public OpenSubtitlesSession(string token, string host)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(token);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(host);
        Token = token;
        Host = host;
    }
}