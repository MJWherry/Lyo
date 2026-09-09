using System.Net;
using Lyo.Exceptions;

namespace Lyo.Http.Client.Session;

/// <summary>Default <see cref="ILyoHttpCookieJar" />. Keeps its own list so export/clear work on netstandard2.0 (no <c>CookieContainer.GetAllCookies</c>).</summary>
public sealed class LyoHttpCookieJar : ILyoHttpCookieJar
{
    private readonly object _gate = new();
    private readonly List<LyoHttpCookie> _cookies = [];
    private readonly CookieContainer _container = new();

    /// <summary>Underlying container for code that still talks to <see cref="HttpClientHandler" />.</summary>
    public CookieContainer Container => _container;

    /// <inheritdoc />
    public IReadOnlyList<LyoHttpCookie> GetCookies(Uri? uri = null)
    {
        lock (_gate) {
            if (uri == null)
                return _cookies.ToArray();

            return _cookies.Where(c => MatchesUri(c, uri)).ToArray();
        }
    }

    /// <inheritdoc />
    public void Add(LyoHttpCookie cookie, Uri? uri = null)
    {
        ArgumentHelpers.ThrowIfNull(cookie);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(cookie.Name);
        var normalized = Normalize(cookie, uri);
        lock (_gate) {
            _cookies.RemoveAll(c => SameIdentity(c, normalized));
            _cookies.Add(normalized);
            TryAddToContainer(normalized, uri);
        }
    }

    /// <inheritdoc />
    public void AddRange(IEnumerable<LyoHttpCookie> cookies, Uri? uri = null)
    {
        ArgumentHelpers.ThrowIfNull(cookies);
        foreach (var cookie in cookies)
            Add(cookie, uri);
    }

    /// <inheritdoc />
    public void Remove(string name, string? domain = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        lock (_gate) {
            _cookies.RemoveAll(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)
                && (domain == null || string.Equals(c.Domain, domain, StringComparison.OrdinalIgnoreCase)));
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
            _cookies.Clear();
    }

    /// <inheritdoc />
    public string ToCookieHeader(Uri uri)
    {
        ArgumentHelpers.ThrowIfNull(uri);
        lock (_gate) {
            var matching = _cookies.Where(c => MatchesUri(c, uri)).Select(c => $"{c.Name}={c.Value}");
            return string.Join("; ", matching);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<LyoHttpCookie> Export() => GetCookies();

    /// <inheritdoc />
    public void Import(IEnumerable<LyoHttpCookie> cookies) => AddRange(cookies);

    private static LyoHttpCookie Normalize(LyoHttpCookie cookie, Uri? uri)
        => new() {
            Name = cookie.Name,
            Value = cookie.Value,
            Domain = string.IsNullOrWhiteSpace(cookie.Domain) ? uri?.Host : cookie.Domain,
            Path = string.IsNullOrWhiteSpace(cookie.Path) ? "/" : cookie.Path,
            Secure = cookie.Secure ?? (uri?.Scheme == Uri.UriSchemeHttps),
            HttpOnly = cookie.HttpOnly,
            Expiry = cookie.Expiry
        };

    private static bool SameIdentity(LyoHttpCookie a, LyoHttpCookie b)
        => string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Domain ?? "", b.Domain ?? "", StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Path ?? "/", b.Path ?? "/", StringComparison.OrdinalIgnoreCase);

    private static bool MatchesUri(LyoHttpCookie cookie, Uri uri)
    {
        if (cookie.Secure == true && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        var path = cookie.Path ?? "/";
        if (!uri.AbsolutePath.StartsWith(path, StringComparison.Ordinal))
            return false;

        if (string.IsNullOrWhiteSpace(cookie.Domain))
            return true;

        var host = uri.Host;
        var domain = cookie.Domain!.Trim().TrimStart('.');
        return host.Equals(domain, StringComparison.OrdinalIgnoreCase) || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
    }

    private void TryAddToContainer(LyoHttpCookie cookie, Uri? uri)
    {
        if (string.IsNullOrWhiteSpace(cookie.Domain) && uri == null)
            return;

        try {
            var net = new Cookie(cookie.Name, cookie.Value, cookie.Path ?? "/", cookie.Domain ?? uri!.Host) {
                Secure = cookie.Secure ?? false,
                HttpOnly = cookie.HttpOnly ?? false
            };
            if (cookie.Expiry is { } expiry)
                net.Expires = expiry.UtcDateTime;

            if (uri != null)
                _container.Add(uri, net);
            else
                _container.Add(net);
        }
        catch (CookieException) {
            // Domain/path rejected by CookieContainer; the list still holds the cookie for header emission.
        }
    }
}
