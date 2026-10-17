using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Http.Client;
using Lyo.Json;
using Lyo.Espn.Fantasy.Football.Client.Models.Request;
using Lyo.Espn.Fantasy.Football.Client.Models.Response;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>HTTP client for ESPN fantasy football league data.</summary>
public class FantasyFootballClient : LyoHttpClient
{
    public readonly CommunicationManager Communication;
    public readonly LeagueManager League;
    public readonly PlayerManager Players;
    public readonly TransactionsManager Transactions;
    private readonly FantasyFootballClientOptions _options;

    /// <summary>Members and teams from the last <see cref="Remember" /> or league <c>GetAsync</c>. Replaced as a whole, not updated in place.</summary>
    public LeagueIndex? LeagueDirectory { get; private set; }

    /// <summary>Player cards filled by player, free-agent, and cached lookups.</summary>
    public PlayerCache PlayerCache { get; } = new();

    public FantasyFootballClient(FantasyFootballClientOptions options, ILoggerFactory? loggerFactory = null, HttpClient? httpClient = null)
        : base(
            loggerFactory?.CreateLogger<FantasyFootballClient>() ?? NullLoggerFactory.Instance.CreateLogger<FantasyFootballClient>(), httpClient,
            LyoJsonSerializerOptions.Create(o => o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull), options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.BaseUrl, nameof(options.BaseUrl));
        _options = options;
        League = new(this);
        Communication = new(this);
        Players = new(this);
        Transactions = new(this);
    }

    /// <summary>Replaces <see cref="LeagueDirectory" /> with the members and teams on <paramref name="league" />.</summary>
    public void Remember(LeagueRes league)
    {
        ArgumentHelpers.ThrowIfNull(league);
        LeagueDirectory = new LeagueIndex(league);
    }

    internal string? Swid => _options.Swid;

    internal string NewsBaseUrl => _options.NewsBaseUrl;

    internal static void ValidateSeason(int seasonId, string methodName)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(seasonId);
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

    internal string BuildCommunicationPath(int leagueId, int seasonId, string view, string? topicId = null)
    {
        ValidateSeason(seasonId, nameof(BuildCommunicationPath));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.CommunicationBaseUrl);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(view);
        var path = $"{_options.CommunicationBaseUrl.TrimEnd('/')}/seasons/{seasonId}/segments/0/leagues/{leagueId}/communication/";
        if (!string.IsNullOrWhiteSpace(topicId))
            path += $"topics/{Uri.EscapeDataString(topicId)}";

        return $"{path}?view={Uri.EscapeDataString(view)}&platform=chat";
    }

    internal async Task<TResult?> GetCommunicationAsync<TResult>(string url, object fantasyFilter, CancellationToken ct)
        => await GetAsAsync<TResult>(
                url, request => {
                    ApplyAuthentication(request);
                    request.Headers.TryAddWithoutValidation("x-fantasy-filter", JsonSerializer.Serialize(fantasyFilter, GetSerializerOptions()));
                }, ct)
            .ConfigureAwait(false);

    internal string BuildWritesPath(int leagueId, int seasonId)
    {
        ValidateSeason(seasonId, nameof(BuildWritesPath));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.WritesBaseUrl);
        return $"{_options.WritesBaseUrl.TrimEnd('/')}/seasons/{seasonId}/segments/0/leagues/{leagueId}/transactions/";
    }

    internal string BuildChatMessagePath(int leagueId, int seasonId, string topicId)
    {
        ValidateSeason(seasonId, nameof(BuildChatMessagePath));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(_options.CommunicationBaseUrl);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(topicId);
        var swid = _options.Swid;
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(swid);
        var topic = Uri.EscapeDataString(topicId);
        var source = Uri.EscapeDataString(swid);
        return $"{_options.CommunicationBaseUrl.TrimEnd('/')}/seasons/{seasonId}/segments/0/leagues/{leagueId}/communication/topics/{topic}/messages/?source={source}";
    }

    internal Task<TResult?> GetAuthenticatedAsync<TResult>(string url, CancellationToken ct)
        => GetAsAsync<TResult>(url, ApplyAuthentication, ct);

    internal Task<TResult> PostAuthenticatedAsync<TRequest, TResult>(string url, TRequest body, CancellationToken ct)
        => PostAsAsync<TRequest, TResult>(url, body, ApplyAuthentication, ct);

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