using Lyo.Exceptions;
using Lyo.Http.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Trakt.Client;

/// <summary>
/// Trakt catalog client. Every catalog request sends <c>trakt-api-version: 2</c> and <c>trakt-api-key</c>. OAuth calls go to <c>https://auth.trakt.tv</c> and do not store a
/// user bearer.
/// </summary>
public class TraktClient : LyoHttpClient
{
    /// <summary>OAuth on <c>https://auth.trakt.tv</c>.</summary>
    public AuthManager Auth { get; }

    /// <summary>Text search and id lookup.</summary>
    public SearchManager Search { get; }

    /// <summary>Movie catalog calls.</summary>
    public MovieManager Movies { get; }

    /// <summary>Show catalog calls.</summary>
    public ShowManager Shows { get; }

    internal TraktClientOptions Settings { get; }

    /// <summary>Creates a client. <paramref name="httpClient" /> is supplied by <c>IHttpClientFactory</c> when the client is resolved from DI.</summary>
    public TraktClient(TraktClientOptions options, ILoggerFactory? loggerFactory = null, HttpClient? httpClient = null)
        : base(loggerFactory?.CreateLogger<TraktClient>() ?? NullLoggerFactory.Instance.CreateLogger<TraktClient>(), httpClient, TraktJson.Create(), options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ClientId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.BaseUrl);
        Settings = options;
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("trakt-api-version", "2");
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("trakt-api-key", options.ClientId);
        Auth = new(this);
        Search = new(this);
        Movies = new(this);
        Shows = new(this);
    }

    internal async Task<TraktPage<T>> GetPageAsync<T>(string relativeUri, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Get, relativeUri);
        using var response = await SendAsync(request, null, ct).ConfigureAwait(false);
        var items = await DeserializeAsync<List<T>>(response, ct).ConfigureAwait(false);
        return new() {
            Items = items ?? [],
            Page = ReadHeader(response, "X-Pagination-Page"),
            Limit = ReadHeader(response, "X-Pagination-Limit"),
            PageCount = ReadHeader(response, "X-Pagination-Page-Count"),
            ItemCount = ReadHeader(response, "X-Pagination-Item-Count")
        };
    }

    private static int ReadHeader(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues(name, out var values))
            return 0;

        var raw = values.FirstOrDefault();
        return int.TryParse(raw, out var parsed) ? parsed : 0;
    }
}