namespace Lyo.Http.Client.Session;

/// <summary>Default <see cref="ILyoHttpSession" />.</summary>
public class LyoHttpSession : ILyoHttpSession
{
    /// <summary>Creates a session with a new id and jar.</summary>
    public LyoHttpSession(string? sessionId = null, ILyoHttpCookieJar? cookieJar = null)
    {
        SessionId = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId!;
        CookieJar = cookieJar ?? new LyoHttpCookieJar();
    }

    /// <inheritdoc />
    public string SessionId { get; }

    /// <inheritdoc />
    public ILyoHttpCookieJar CookieJar { get; }

    /// <inheritdoc />
    public string? UserAgent { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
