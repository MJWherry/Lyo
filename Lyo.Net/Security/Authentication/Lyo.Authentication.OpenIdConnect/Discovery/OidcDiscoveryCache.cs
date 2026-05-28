using System.Collections.Concurrent;
using System.Net.Http.Json;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.OpenIdConnect.Discovery;

/// <summary>
/// In-memory, time-bounded cache of OIDC discovery documents. Reduces traffic to the provider's <c>/.well-known/openid-configuration</c> endpoint. Hosts that scale out share
/// nothing (each replica fetches once per TTL).
/// </summary>
public sealed class OidcDiscoveryCache
{
    /// <summary>The default time-to-live for a cached discovery document.</summary>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
    private readonly HttpClient _http;
    private readonly ILogger<OidcDiscoveryCache> _logger;
    private readonly TimeSpan _ttl;

    /// <summary>Creates a new cache backed by the supplied <paramref name="http" />. TTL defaults to <see cref="DefaultTtl" />.</summary>
    public OidcDiscoveryCache(HttpClient http, ILogger<OidcDiscoveryCache> logger, TimeSpan? ttl = null)
    {
        ArgumentHelpers.ThrowIfNull(http);
        ArgumentHelpers.ThrowIfNull(logger);
        _http = http;
        _logger = logger;
        _ttl = ttl ?? DefaultTtl;
    }

    /// <summary>Fetches the document for the given discovery URL, returning a cached copy when fresh.</summary>
    public async Task<OidcDiscoveryDocument> GetAsync(string discoveryUrl, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(discoveryUrl);
        var now = DateTime.UtcNow;
        if (_cache.TryGetValue(discoveryUrl, out var entry) && entry.ExpiresAt > now)
            return entry.Document;

        _logger.LogDebug("Fetching OIDC discovery document from {Url}", discoveryUrl);
        var document = await _http.GetFromJsonAsync<OidcDiscoveryDocument>(discoveryUrl, ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"OIDC discovery at '{discoveryUrl}' returned no body.");

        _cache[discoveryUrl] = new(document, now + _ttl);
        return document;
    }

    /// <summary>Removes the cached entry for the supplied discovery URL (forces a refresh next call). Returns <c>true</c> when an entry existed.</summary>
    public bool Invalidate(string discoveryUrl)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(discoveryUrl);
        return _cache.TryRemove(discoveryUrl, out var _);
    }

    private sealed record CacheEntry(OidcDiscoveryDocument Document, DateTime ExpiresAt);
}