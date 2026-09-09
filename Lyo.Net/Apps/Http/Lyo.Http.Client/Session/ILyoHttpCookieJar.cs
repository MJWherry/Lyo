namespace Lyo.Http.Client.Session;

/// <summary>Session-owned cookie store backed by <see cref="System.Net.CookieContainer" />. Not recreated per pooled handler spin-up.</summary>
public interface ILyoHttpCookieJar
{
    /// <summary>Cookies for <paramref name="uri" />, or the whole jar when uri is null.</summary>
    IReadOnlyList<LyoHttpCookie> GetCookies(Uri? uri = null);

    /// <summary>Adds or replaces a cookie. <paramref name="uri" /> supplies default domain/path when the cookie omits them.</summary>
    void Add(LyoHttpCookie cookie, Uri? uri = null);

    /// <summary>Adds each cookie in <paramref name="cookies" />.</summary>
    void AddRange(IEnumerable<LyoHttpCookie> cookies, Uri? uri = null);

    /// <summary>Removes cookies matching <paramref name="name" /> (and optional domain).</summary>
    void Remove(string name, string? domain = null);

    /// <summary>Empties the jar.</summary>
    void Clear();

    /// <summary>RFC 6265 Cookie header value for <paramref name="uri" />.</summary>
    string ToCookieHeader(Uri uri);

    /// <summary>JSON-serializable snapshot for plan <c>exportCookies</c> (do not commit secrets).</summary>
    IReadOnlyList<LyoHttpCookie> Export();

    /// <summary>Merges a previously exported list.</summary>
    void Import(IEnumerable<LyoHttpCookie> cookies);
}
