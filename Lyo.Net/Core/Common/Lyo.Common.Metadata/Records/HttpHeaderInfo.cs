using System.Diagnostics;
using Lyo.Common.Core;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Core.Net;

namespace Lyo.Common.Metadata.Records;

/// <summary>
/// Curated HTTP header catalog: RFC names, Lyo contract headers, and common <c>X-</c> names. Vendor wire names belong in that vendor’s
/// <c>.Models</c> package, not here. Implicitly converts to <see cref="string" /> so <c>Headers.Contains(HttpHeaderInfo.Cookie)</c> stays a one-token call.
/// </summary>
/// <remarks>Not a complete IANA field registry. Lookups are case-insensitive (RFC 9110).</remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record HttpHeaderInfo(string Name, string Description, HttpHeaderCategory Category, HttpHeaderPresence Presence, string[] Aliases)
{
    /// <summary>Sentinel for an unrecognized or unregistered header.</summary>
    public static readonly HttpHeaderInfo Unknown = new(
        "Unknown", "Unknown or unregistered HTTP header", HttpHeaderCategory.Unknown, HttpHeaderPresence.None, ["unknown", "unspecified"]);

    /// <summary><c>Accept</c> — request media types the client will accept.</summary>
    public static readonly HttpHeaderInfo Accept = new("Accept", "Media types the client is willing to receive", HttpHeaderCategory.Negotiation, HttpHeaderPresence.Request, []);

    /// <summary><c>Accept-Language</c> — preferred natural languages.</summary>
    public static readonly HttpHeaderInfo AcceptLanguage = new(
        "Accept-Language", "Natural languages the client prefers", HttpHeaderCategory.Negotiation, HttpHeaderPresence.Request, []);

    /// <summary><c>Accept-Encoding</c> — content-codings the client will accept.</summary>
    public static readonly HttpHeaderInfo AcceptEncoding = new(
        "Accept-Encoding", "Content-codings the client will accept", HttpHeaderCategory.Negotiation, HttpHeaderPresence.Request, []);

    /// <summary><c>User-Agent</c> — originating client product token.</summary>
    public static readonly HttpHeaderInfo UserAgent = new("User-Agent", "Originating client product token", HttpHeaderCategory.Identity, HttpHeaderPresence.Request, []);

    /// <summary><c>Origin</c> — origin of a CORS or fetch request.</summary>
    public static readonly HttpHeaderInfo Origin = new("Origin", "Origin of a CORS or fetch request", HttpHeaderCategory.Identity, HttpHeaderPresence.Request, []);

    /// <summary><c>Referer</c> — URI of the resource from which the request originated (RFC spelling).</summary>
    public static readonly HttpHeaderInfo Referer = new(
        "Referer", "URI of the resource from which the request originated", HttpHeaderCategory.Identity, HttpHeaderPresence.Request, ["Referrer"]);

    /// <summary><c>Content-Type</c> — representation media type.</summary>
    public static readonly HttpHeaderInfo ContentType = new("Content-Type", "Representation media type", HttpHeaderCategory.Representation, HttpHeaderPresence.Both, []);

    /// <summary><c>Cookie</c> — stored cookies the client is sending.</summary>
    public static readonly HttpHeaderInfo Cookie = new("Cookie", "Stored cookies the client is sending", HttpHeaderCategory.Cookie, HttpHeaderPresence.Request, []);

    /// <summary><c>Set-Cookie</c> — cookies the origin wants stored.</summary>
    public static readonly HttpHeaderInfo SetCookie = new("Set-Cookie", "Cookies the origin wants stored", HttpHeaderCategory.Cookie, HttpHeaderPresence.Response, []);

    /// <summary><c>Retry-After</c> — how long to wait before retrying.</summary>
    public static readonly HttpHeaderInfo RetryAfter = new(
        "Retry-After", "How long to wait before retrying a request", HttpHeaderCategory.Routing, HttpHeaderPresence.Response, []);

    /// <summary>Standard credential header with a scheme prefix, for example <c>Bearer {token}</c>.</summary>
    public static readonly HttpHeaderInfo Authorization = new(
        LyoHttpHeaders.Authorization, "Credential with a scheme prefix (for example Bearer)", HttpHeaderCategory.Auth, HttpHeaderPresence.Request, []);

    /// <summary>Lyo API key header, carrying the raw secret with no scheme prefix.</summary>
    public static readonly HttpHeaderInfo ApiKey = new(
        LyoHttpHeaders.ApiKey, "Lyo API key with no scheme prefix", HttpHeaderCategory.Auth, HttpHeaderPresence.Request, []);

    /// <summary>Primary correlation-id header written and read by <c>Lyo.Diagnostic</c>.</summary>
    public static readonly HttpHeaderInfo CorrelationId = new(
        LyoHttpHeaders.CorrelationId, "Primary Lyo correlation identifier", HttpHeaderCategory.Correlation, HttpHeaderPresence.Both, []);

    /// <summary>Secondary correlation-id header, accepted for interop with services that only emit this spelling.</summary>
    public static readonly HttpHeaderInfo RequestId = new(
        LyoHttpHeaders.RequestId, "Secondary correlation identifier (X-Request-Id)", HttpHeaderCategory.Correlation, HttpHeaderPresence.Both, ["X-Request-ID"]);

    /// <summary>Lyo tenant routing header used by FileStorage.</summary>
    public static readonly HttpHeaderInfo TenantId = new("X-Tenant-Id", "Tenant identifier for multi-tenant routing", HttpHeaderCategory.Tenant, HttpHeaderPresence.Both, []);

    /// <summary>Auth-client origin of the calling Lyo app.</summary>
    public static readonly HttpHeaderInfo LyoCallerOrigin = new(
        "X-Lyo-Caller-Origin", "Origin of the calling Lyo application", HttpHeaderCategory.Identity, HttpHeaderPresence.Request, []);

    /// <summary><c>X-Trace-Id</c> — distributed-trace identifier used by some proxies and APM stacks.</summary>
    public static readonly HttpHeaderInfo TraceId = new("X-Trace-Id", "Distributed-trace identifier", HttpHeaderCategory.Correlation, HttpHeaderPresence.Both, ["X-Trace-ID"]);

    /// <summary><c>X-Forwarded-For</c> — originating client addresses seen by a reverse proxy.</summary>
    public static readonly HttpHeaderInfo ForwardedFor = new(
        "X-Forwarded-For", "Originating client addresses as seen by a reverse proxy", HttpHeaderCategory.Proxy, HttpHeaderPresence.Request, []);

    /// <summary><c>X-Forwarded-Host</c> — original Host the client sent.</summary>
    public static readonly HttpHeaderInfo ForwardedHost = new("X-Forwarded-Host", "Original Host the client sent", HttpHeaderCategory.Proxy, HttpHeaderPresence.Request, []);

    /// <summary><c>X-Forwarded-Proto</c> — original scheme (<c>http</c> / <c>https</c>).</summary>
    public static readonly HttpHeaderInfo ForwardedProto = new(
        "X-Forwarded-Proto", "Original request scheme (http or https)", HttpHeaderCategory.Proxy, HttpHeaderPresence.Request, []);

    /// <summary><c>X-Forwarded-Prefix</c> — path prefix stripped by a reverse proxy (OIDC path-base).</summary>
    public static readonly HttpHeaderInfo ForwardedPrefix = new(
        "X-Forwarded-Prefix", "Path prefix stripped by a reverse proxy", HttpHeaderCategory.Proxy, HttpHeaderPresence.Request, []);

    /// <summary><c>X-Forwarded-Port</c> — original port the client connected to.</summary>
    public static readonly HttpHeaderInfo ForwardedPort = new(
        "X-Forwarded-Port", "Original port the client connected to", HttpHeaderCategory.Proxy, HttpHeaderPresence.Request, []);

    /// <summary><c>X-Real-IP</c> — single originating client address set by some proxies.</summary>
    public static readonly HttpHeaderInfo RealIp = new(
        "X-Real-IP", "Single originating client address set by some proxies", HttpHeaderCategory.Proxy, HttpHeaderPresence.Request, ["X-Real-Ip"]);

    /// <summary><c>X-CSRF-Token</c> — anti-forgery token for cookie-authenticated browser calls.</summary>
    public static readonly HttpHeaderInfo CsrfToken = new(
        "X-CSRF-Token", "Anti-forgery token for cookie-authenticated browser calls", HttpHeaderCategory.Auth, HttpHeaderPresence.Request, ["X-XSRF-TOKEN", "X-Xsrf-Token"]);

    /// <summary><c>X-Requested-With</c> — legacy AJAX marker, often <c>XMLHttpRequest</c>.</summary>
    public static readonly HttpHeaderInfo RequestedWith = new(
        "X-Requested-With", "Legacy AJAX marker (typically XMLHttpRequest)", HttpHeaderCategory.Auth, HttpHeaderPresence.Request, []);

    /// <summary><c>X-Content-Type-Options</c> — sniffing policy, typically <c>nosniff</c>.</summary>
    public static readonly HttpHeaderInfo ContentTypeOptions = new(
        "X-Content-Type-Options", "MIME-sniffing policy (typically nosniff)", HttpHeaderCategory.Security, HttpHeaderPresence.Response, []);

    /// <summary><c>X-Frame-Options</c> — clickjacking policy (<c>DENY</c> / <c>SAMEORIGIN</c>).</summary>
    public static readonly HttpHeaderInfo FrameOptions = new(
        "X-Frame-Options", "Clickjacking policy (DENY or SAMEORIGIN)", HttpHeaderCategory.Security, HttpHeaderPresence.Response, []);

    /// <summary><c>X-XSS-Protection</c> — legacy XSS filter switch; still seen on older stacks.</summary>
    public static readonly HttpHeaderInfo XssProtection = new(
        "X-XSS-Protection", "Legacy XSS-filter switch", HttpHeaderCategory.Security, HttpHeaderPresence.Response, []);

    /// <summary><c>X-RateLimit-Limit</c> — request quota for the current window.</summary>
    public static readonly HttpHeaderInfo RateLimitLimit = new(
        "X-RateLimit-Limit", "Request quota for the current window", HttpHeaderCategory.RateLimit, HttpHeaderPresence.Response, ["X-Rate-Limit-Limit"]);

    /// <summary><c>X-RateLimit-Remaining</c> — remaining requests in the current window.</summary>
    public static readonly HttpHeaderInfo RateLimitRemaining = new(
        "X-RateLimit-Remaining", "Remaining requests in the current window", HttpHeaderCategory.RateLimit, HttpHeaderPresence.Response, ["X-Rate-Limit-Remaining"]);

    /// <summary><c>X-RateLimit-Reset</c> — when the current window resets (unix timestamp or delta).</summary>
    public static readonly HttpHeaderInfo RateLimitReset = new(
        "X-RateLimit-Reset", "When the current quota window resets", HttpHeaderCategory.RateLimit, HttpHeaderPresence.Response, ["X-Rate-Limit-Reset"]);

    /// <summary><c>Idempotency-Key</c> — client-supplied replay token for unsafe methods.</summary>
    public static readonly HttpHeaderInfo IdempotencyKey = new(
        "Idempotency-Key", "Client-supplied replay token for unsafe methods", HttpHeaderCategory.Idempotency, HttpHeaderPresence.Request, ["X-Idempotency-Key"]);

    private static readonly Dictionary<string, HttpHeaderInfo> ByAlias = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<HttpHeaderInfo> AllHeaders = [];

    /// <summary>Registered headers, excluding <see cref="Unknown" />.</summary>
    public static IReadOnlyList<HttpHeaderInfo> All => AllHeaders;

    /// <summary>True when the header is valid on requests.</summary>
    public bool IsRequest => Presence.HasFlag(HttpHeaderPresence.Request);

    /// <summary>True when the header is valid on responses.</summary>
    public bool IsResponse => Presence.HasFlag(HttpHeaderPresence.Response);

    static HttpHeaderInfo()
    {
        var fields = typeof(HttpHeaderInfo).PublicStaticFields<HttpHeaderInfo>();

        foreach (var info in fields) {
            if (info == Unknown)
                continue;

            AllHeaders.Add(info);
            RegisterAlias(info.Name, info);
            foreach (var alias in info.Aliases) {
                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterAlias(alias, info);
            }
        }
    }

    /// <summary>Looks up a header by wire name or alias (case-insensitive), or <see cref="Unknown" /> when it is not registered.</summary>
    /// <param name="name">Header name, for example <c>User-Agent</c> or <c>x-correlation-id</c>.</param>
    /// <returns>The catalog row, or <see cref="Unknown" />.</returns>
    public static HttpHeaderInfo FromName(string? name)
    {
        if (name.IsNullOrWhitespace())
            return Unknown;

        return ByAlias.TryGetValue(Normalize(name), out var info) ? info : Unknown;
    }

    /// <summary>Attempts to resolve a registered header by wire name or alias.</summary>
    /// <param name="name">Header name to look up.</param>
    /// <param name="info">The catalog row when found; otherwise <see cref="Unknown" />.</param>
    /// <returns><see langword="true" /> when the name is registered.</returns>
    public static bool TryFromName(string? name, out HttpHeaderInfo info)
    {
        if (name.IsNullOrWhitespace()) {
            info = Unknown;
            return false;
        }

        if (ByAlias.TryGetValue(Normalize(name), out var found)) {
            info = found;
            return true;
        }

        info = Unknown;
        return false;
    }

    /// <summary>Lists registered headers in <paramref name="category" />.</summary>
    /// <param name="category">The header purpose bucket.</param>
    /// <returns>Matching catalog rows.</returns>
    public static IEnumerable<HttpHeaderInfo> ByCategory(HttpHeaderCategory category) => AllHeaders.Where(h => h.Category == category);

    /// <summary>Implicit conversion to the wire name so APIs that take <see cref="string" /> can take the catalog row.</summary>
    public static implicit operator string(HttpHeaderInfo info) => info.Name;

    /// <inheritdoc />
    public override string ToString() => Name;

    private static void RegisterAlias(string alias, HttpHeaderInfo info)
    {
        var key = Normalize(alias);
        if (!ByAlias.ContainsKey(key))
            ByAlias[key] = info;
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
