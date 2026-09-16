using Lyo.Exceptions;

namespace Lyo.Http.Client.Pipeline;

/// <summary>
/// Applies headers configured on <see cref="LyoHttpClientOptions" />. A header already on the request is left as-is. A resolver that returns text replaces the static value.
/// </summary>
public sealed class LyoHttpHeaderHandler : DelegatingHandler
{
    private readonly LyoHttpClientOptions _options;
    private readonly IServiceProvider _services;

    /// <summary>Creates the handler. <paramref name="services" /> is the provider passed to header resolvers.</summary>
    public LyoHttpHeaderHandler(LyoHttpClientOptions options, IServiceProvider services)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(services);
        _options = options;
        _services = services;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(request);
        foreach (var pair in _options.ConfiguredHeaders) {
            var value = pair.Value.StaticValue;
            if (pair.Value.Resolve is not null) {
                var resolved = await pair.Value.Resolve(_services, ct).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(resolved))
                    value = resolved;
            }

            if (HasHeader(request, pair.Key) || string.IsNullOrEmpty(value))
                continue;

            request.Headers.TryAddWithoutValidation(pair.Key, value);
        }

        return await base.SendAsync(request, ct).ConfigureAwait(false);
    }

    private static bool HasHeader(HttpRequestMessage request, string name)
        => Contains(request.Headers, name) || (request.Content is not null && Contains(request.Content.Headers, name));

    private static bool Contains(System.Net.Http.Headers.HttpHeaders headers, string name)
    {
        try {
            return headers.Contains(name);
        }
        catch (InvalidOperationException) {
            return false;
        }
    }
}
