using System.Collections.Concurrent;
using System.Net.Http.Json;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;

namespace Lyo.Authentication.OpenIdConnect.Discovery;

/// <summary>Fetches and caches provider JWKS documents. Used by <see cref="Client.OidcIdTokenValidator" /> to validate id_token signatures.</summary>
public sealed class OidcJwksResolver
{
    /// <summary>Default time-to-live for a cached JWKS document.</summary>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
    private readonly HttpClient _http;
    private readonly ILogger<OidcJwksResolver> _logger;
    private readonly TimeSpan _ttl;

    /// <summary>Creates a new resolver.</summary>
    public OidcJwksResolver(HttpClient http, ILogger<OidcJwksResolver> logger, TimeSpan? ttl = null)
    {
        ArgumentHelpers.ThrowIfNull(http);
        ArgumentHelpers.ThrowIfNull(logger);
        _http = http;
        _logger = logger;
        _ttl = ttl ?? DefaultTtl;
    }

    /// <summary>Resolves a key by <c>kid</c> from the given JWKS URI. Forces a refresh once if the kid isn't present (handles mid-rotation).</summary>
    public async Task<OidcJsonWebKey?> ResolveAsync(string jwksUri, string kid, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jwksUri);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(kid);
        var doc = await GetAsync(jwksUri, ct).ConfigureAwait(false);
        var match = doc.Keys.FirstOrDefault(k => string.Equals(k.Kid, kid, StringComparison.Ordinal));
        if (match is not null)
            return match;

        Invalidate(jwksUri);
        doc = await GetAsync(jwksUri, ct).ConfigureAwait(false);
        return doc.Keys.FirstOrDefault(k => string.Equals(k.Kid, kid, StringComparison.Ordinal));
    }

    /// <summary>Removes the cached JWKS for the given URI.</summary>
    public bool Invalidate(string jwksUri)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(jwksUri);
        return _cache.TryRemove(jwksUri, out var _);
    }

    private async Task<OidcJwksDocument> GetAsync(string jwksUri, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (_cache.TryGetValue(jwksUri, out var entry) && entry.ExpiresAt > now)
            return entry.Document;

        _logger.LogDebug("Fetching JWKS from {Url}", jwksUri);
        var doc = await _http.GetFromJsonAsync<OidcJwksDocument>(jwksUri, ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"JWKS at '{jwksUri}' returned no body.");

        _cache[jwksUri] = new(doc, now + _ttl);
        return doc;
    }

    private sealed record CacheEntry(OidcJwksDocument Document, DateTime ExpiresAt);
}