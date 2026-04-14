using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Api.Client;
using Lyo.Espn.Fantasy.Football.Models.Request;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Espn.Fantasy.Football;

/// <summary>Client for ESPN fantasy football league data.</summary>
public class FantasyFootballClient : ApiClient
{
    private readonly FantasyFootballClientOptions _options;

    public readonly CommunicationManager Communication;
    public readonly LeagueManager League;
    public readonly PlayerManager Players;
    public readonly TransactionsManager Transactions;

    public FantasyFootballClient(FantasyFootballClientOptions options, ILoggerFactory? loggerFactory = null, HttpClient? httpClient = null)
        : base(
            loggerFactory?.CreateLogger<FantasyFootballClient>() ?? NullLoggerFactory.Instance.CreateLogger<FantasyFootballClient>(),
            httpClient ?? new HttpClient { BaseAddress = new(options.ApiBaseUrl) },
            new() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            }, new() { BaseUrl = options.ApiBaseUrl, EnsureStatusCode = options.EnsureStatusCode })
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ApiBaseUrl, nameof(options.ApiBaseUrl));
        _options = options;
        League = new(this);
        Communication = new(this);
        Players = new(this);
        Transactions = new(this);
    }

    internal static void ValidateSeason(int seasonId, string methodName)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(seasonId, nameof(seasonId));
        ArgumentHelpers.ThrowIf(seasonId < 2018, $"{methodName} currently supports ESPN v3 seasons from 2018 onward.", nameof(seasonId));
    }

    internal string BuildLeaguePath(int leagueId, int seasonId, IEnumerable<string>? views = null, IReadOnlyDictionary<string, string?>? query = null, string? suffix = null)
    {
        ValidateSeason(seasonId, nameof(BuildLeaguePath));
        var path = $"seasons/{seasonId}/segments/0/leagues/{leagueId}";
        if (!string.IsNullOrWhiteSpace(suffix))
            path += suffix;

        var queryParts = new List<string>();
        if (views != null) {
            foreach (var view in views.Where(i => !string.IsNullOrWhiteSpace(i)))
                queryParts.Add($"view={Uri.EscapeDataString(view)}");
        }

        if (query != null) {
            foreach (var pair in query.Where(i => !string.IsNullOrWhiteSpace(i.Key) && i.Value != null))
                queryParts.Add($"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}");
        }

        return queryParts.Count == 0 ? path : $"{path}?{string.Join("&", queryParts)}";
    }

    internal async Task<TResult?> GetLeagueViewAsync<TResult>(string path, FantasyFilterReq? xFantasyFilter = null, CancellationToken ct = default)
        => await GetAsAsync<TResult?>(
                path, request => {
                    ApplyAuthentication(request);
                    if (xFantasyFilter != null)
                        request.Headers.TryAddWithoutValidation("x-fantasy-filter", JsonSerializer.Serialize(xFantasyFilter, GetSerializerOptions()));
                }, ct)
            .ConfigureAwait(false);

    private void ApplyAuthentication(HttpRequestMessage request)
    {
        var cookies = new List<string>();
        if (!string.IsNullOrWhiteSpace(_options.EspnS2))
            cookies.Add($"espn_s2={_options.EspnS2}");

        if (!string.IsNullOrWhiteSpace(_options.Swid))
            cookies.Add($"SWID={_options.Swid}");

        if (cookies.Count > 0)
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies) + ";");
    }
}