namespace Lyo.Http.Client.Session;

/// <summary>Logical HTTP session: cookie jar, pinned User-Agent, and an items bag. One per typed-client scope or <c>CreateSession()</c>.</summary>
public interface ILyoHttpSession
{
    /// <summary>Stable id for this session (used as the User-Agent rotation seed when PerSession).</summary>
    string SessionId { get; }

    /// <summary>Cookies owned by this session, not by a pooled handler.</summary>
    ILyoHttpCookieJar CookieJar { get; }

    /// <summary>User-Agent pinned for this session. Flared overwrites this with <c>solution.userAgent</c> after a solve.</summary>
    string? UserAgent { get; set; }

    /// <summary>Arbitrary per-session values (plan bindings, solver session id, and so on).</summary>
    IDictionary<string, object?> Items { get; }
}
