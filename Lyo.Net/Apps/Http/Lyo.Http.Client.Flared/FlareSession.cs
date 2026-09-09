using Lyo.Http.Client.Session;

namespace Lyo.Http.Client.Flared;

/// <summary>Default <see cref="IFlareSession" />.</summary>
public sealed class FlareSession : LyoHttpSession, IFlareSession
{
    /// <summary>Creates a Flared session.</summary>
    public FlareSession(string? sessionId = null, ILyoHttpCookieJar? cookieJar = null)
        : base(sessionId, cookieJar) { }

    /// <inheritdoc />
    public string? FlareSolverrSessionId { get; set; }
}
