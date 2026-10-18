using System.Collections.Concurrent;
using Lyo.Authentication.Sso.Completion;
using Lyo.Exceptions;

namespace Lyo.Authentication.Saml;

/// <summary>
/// Fetches and caches IdP metadata. HTTPS only. A redirect to another host or to HTTP is refused.
/// A failed fetch leaves the last good entry in place.
/// </summary>
public sealed class SamlMetadataCache(HttpClient http) : ISamlMetadataSource
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private Func<DateTime> _utcNow = () => DateTime.UtcNow;

    /// <summary>Replaces the clock. Tests use this to expire a cached entry.</summary>
    internal void UseClock(Func<DateTime> utcNow)
    {
        ArgumentHelpers.ThrowIfNull(utcNow);
        _utcNow = utcNow;
    }

    /// <inheritdoc />
    public async Task<SamlIdpMetadata> GetAsync(ISamlIdentityProvider provider, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(provider);
        var now = _utcNow();
        _entries.TryGetValue(provider.Name, out var cached);
        if (cached is not null && cached.ExpiresAt > now)
            return cached.Metadata;

        try
        {
            var fresh = await FetchAsync(provider.MetadataUrl, ct).ConfigureAwait(false);
            _entries[provider.Name] = new Entry(fresh, now.AddHours(1));
            return fresh;
        }
        catch (Exception ex) when (ex is not OperationCanceledException && cached is not null)
        {
            return cached.Metadata;
        }
    }

    private async Task<SamlIdpMetadata> FetchAsync(string metadataUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(metadataUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ExternalLoginRejectedException("SamlMetadata", "IdP metadata URL must be HTTPS.");

        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (IsCrossHostOrHttpRedirect(uri, response))
            throw new ExternalLoginRejectedException("SamlMetadata", "IdP metadata refused a redirect to another host.");

        var finalUri = response.RequestMessage?.RequestUri ?? uri;
        if (finalUri.Scheme != Uri.UriSchemeHttps || !finalUri.Host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase))
            throw new ExternalLoginRejectedException("SamlMetadata", "IdP metadata refused a redirect to another host.");

        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > SamlConstants.MaxDocumentBytes)
            throw new ExternalLoginRejectedException("SamlMetadata", "IdP metadata exceeds the size cap.");

        var xml = await ReadCappedAsync(response, ct).ConfigureAwait(false);
        return SamlMetadataParser.Parse(xml);
    }

    private static bool IsCrossHostOrHttpRedirect(Uri requested, HttpResponseMessage response)
    {
        if ((int)response.StatusCode is < 300 or >= 400)
            return false;

        var location = response.Headers.Location;
        if (location is null)
            return true;
        if (!location.IsAbsoluteUri)
            location = new Uri(requested, location);
        return location.Scheme != Uri.UriSchemeHttps || !location.Host.Equals(requested.Host, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ReadCappedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var limited = new MemoryStream();
        var buffer = new byte[8192];
        var total = 0;
        while (true)
        {
            var read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (read == 0)
                break;
            total += read;
            if (total > SamlConstants.MaxDocumentBytes)
                throw new ExternalLoginRejectedException("SamlMetadata", "IdP metadata exceeds the size cap.");
            limited.Write(buffer, 0, read);
        }

        return System.Text.Encoding.UTF8.GetString(limited.ToArray());
    }

    private sealed record Entry(SamlIdpMetadata Metadata, DateTime ExpiresAt);
}
