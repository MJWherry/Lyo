using Lyo.Api.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.FantasyFootball.Web.Components;

/// <summary>Chat and trade workbench. Reads and writes the FantasyFootball HTTP API.</summary>
public partial class FantasyFootballWorkbench
{
    [Inject]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>ESPN league id.</summary>
    [Parameter]
    [EditorRequired]
    public int LeagueId { get; set; }

    /// <summary>ESPN season, such as 2026.</summary>
    [Parameter]
    [EditorRequired]
    public int SeasonId { get; set; }

    /// <summary>Route prefix. Defaults to <c>FantasyFootball</c>.</summary>
    [Parameter]
    public string RoutePrefix { get; set; } = "FantasyFootball";

    /// <summary>ESPN <c>espn_s2</c> cookie. Blank keeps the cookie configured on the API host.</summary>
    [Parameter]
    public string? EspnS2 { get; set; }

    /// <summary>ESPN <c>SWID</c> cookie. Blank keeps the cookie configured on the API host.</summary>
    [Parameter]
    public string? Swid { get; set; }

    private readonly HashSet<int> _send = [];
    private readonly HashSet<int> _receive = [];
    private IReadOnlyList<FantasyConversationRecord> _conversations = [];
    private IReadOnlyList<FantasyMessageRecord> _messages = [];
    private IReadOnlyList<FantasyTradeRecord> _trades = [];
    private IReadOnlyList<FantasyTeamRecord> _teams = [];
    private string? _selectedTopicId;
    private string? _draft;
    private string? _comment;
    private string? _error;
    private string _leagueLabel = "";
    private int _teamId;
    private int _otherTeamId;
    private int _scoringPeriodId = 1;
    private bool _busy;
    private int _loadedLeagueId;
    private int _loadedSeasonId;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        _leagueLabel = $"{LeagueId} / {SeasonId}";
        if (_loadedLeagueId == LeagueId && _loadedSeasonId == SeasonId)
            return;

        _loadedLeagueId = LeagueId;
        _loadedSeasonId = SeasonId;
        try {
            await LoadAsync();
        }
        catch (Exception ex) {
            _error = ex.Message;
        }
    }

    private async Task RefreshAsync()
    {
        _busy = true;
        _error = null;
        try {
            await ApiClient.PostAsAsync<FantasyLeagueRecord>(Path("refresh"), Authenticate);
            Snackbar.Add("Refreshed from ESPN.", Severity.Success);
            await LoadAsync();
        }
        catch (Exception ex) {
            _error = ex.Message;
        }
        finally {
            _busy = false;
        }
    }

    private async Task LoadAsync()
    {
        var league = await ApiClient.GetAsAsync<FantasyLeagueRecord>(Path(""));
        _teams = league?.Teams ?? [];
        _scoringPeriodId = league?.ScoringPeriodId > 0 ? league.ScoringPeriodId : 1;
        if (_teams.Count > 0 && _teamId == 0)
            _teamId = _teams[0].EspnTeamId;
        if (_teams.Count > 1 && _otherTeamId == 0)
            _otherTeamId = _teams[1].EspnTeamId;

        _conversations = await ApiClient.GetAsAsync<List<FantasyConversationRecord>>(Path("conversations")) ?? [];
        _trades = await ApiClient.GetAsAsync<List<FantasyTradeRecord>>(Path("trades")) ?? [];
        if (_selectedTopicId == null && _conversations.Count > 0)
            await OpenConversationAsync(_conversations[0].EspnTopicId);
    }

    private async Task OpenConversationAsync(string topicId)
    {
        _selectedTopicId = topicId;
        _messages = await ApiClient.GetAsAsync<List<FantasyMessageRecord>>(Path($"conversations/{Uri.EscapeDataString(topicId)}/messages")) ?? [];
    }

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(_selectedTopicId) || string.IsNullOrWhiteSpace(_draft))
            return;

        _busy = true;
        _error = null;
        try {
            await ApiClient.PostAsAsync<PostChatBody, FantasyMessageRecord>(Path($"conversations/{Uri.EscapeDataString(_selectedTopicId)}/messages"), new PostChatBody(_draft), Authenticate);
            _draft = "";
            await OpenConversationAsync(_selectedTopicId);
        }
        catch (Exception ex) {
            _error = ex.Message;
        }
        finally {
            _busy = false;
        }
    }

    private async Task ProposeAsync()
    {
        _busy = true;
        _error = null;
        try {
            await ApiClient.PostAsAsync<ProposeBody, FantasyTradeRecord>(Path("trades"), new ProposeBody(_teamId, _otherTeamId, _scoringPeriodId, _send.ToArray(), _receive.ToArray(), _comment), Authenticate);
            _send.Clear();
            _receive.Clear();
            _comment = null;
            _trades = await ApiClient.GetAsAsync<List<FantasyTradeRecord>>(Path("trades")) ?? [];
            Snackbar.Add("Trade proposed.", Severity.Success);
        }
        catch (Exception ex) {
            _error = ex.Message;
        }
        finally {
            _busy = false;
        }
    }

    private async Task RespondAsync(FantasyTradeRecord trade, string action)
    {
        _busy = true;
        _error = null;
        try {
            await ApiClient.PostAsAsync<RespondBody, FantasyTradeRecord>(Path($"trades/{Uri.EscapeDataString(trade.EspnTransactionId)}/respond"), new RespondBody(_teamId, _scoringPeriodId, action, null), Authenticate);
            _trades = await ApiClient.GetAsAsync<List<FantasyTradeRecord>>(Path("trades")) ?? [];
        }
        catch (Exception ex) {
            _error = ex.Message;
        }
        finally {
            _busy = false;
        }
    }

    private IEnumerable<FantasyRosterEntryRecord> Roster(int teamId)
        => _teams.FirstOrDefault(team => team.EspnTeamId == teamId)?.Roster ?? [];

    private static string TeamLabel(FantasyTeamRecord team) => string.IsNullOrWhiteSpace(team.Name) ? $"Team {team.EspnTeamId}" : team.Name;

    private static string PlayerLabel(FantasyRosterEntryRecord player) => string.IsNullOrWhiteSpace(player.PlayerName) ? player.PlayerId.ToString() : player.PlayerName;

    private static string MessageBody(FantasyMessageRecord message)
    {
        if (!string.IsNullOrWhiteSpace(message.Content))
            return message.Content;

        if (message.Players.Count == 0)
            return message.Kind;

        return string.Join(", ", message.Players.Select(player => $"{player.Direction} {player.PlayerName ?? player.PlayerId.ToString()}"));
    }

    private static void Toggle(HashSet<int> ids, int playerId, bool selected)
    {
        if (selected)
            ids.Add(playerId);
        else
            ids.Remove(playerId);
    }

    private void Authenticate(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(EspnS2))
            request.Headers.TryAddWithoutValidation("X-Espn-S2", EspnS2);
        if (!string.IsNullOrWhiteSpace(Swid))
            request.Headers.TryAddWithoutValidation("X-Swid", Swid);
    }

    private string Path(string relative)
    {
        var prefix = RoutePrefix.Trim().Trim('/');
        var tail = relative.Trim('/');
        var root = $"{prefix}/{LeagueId}/{SeasonId}";
        return string.IsNullOrEmpty(tail) ? root : $"{root}/{tail}";
    }

    private sealed record PostChatBody(string Content);

    private sealed record ProposeBody(int TeamId, int OtherTeamId, int ScoringPeriodId, int[] SendPlayerIds, int[] ReceivePlayerIds, string? Comment);

    private sealed record RespondBody(int TeamId, int ScoringPeriodId, string Action, string? Comment);
}
