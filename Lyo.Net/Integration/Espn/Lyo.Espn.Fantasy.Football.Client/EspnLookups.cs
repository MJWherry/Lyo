using Lyo.Espn.Fantasy.Football.Client.Models.Response;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>One transaction item plus the teams and player its ids refer to.</summary>
public sealed record TransactionItemView(TransactionItemRes Item, TeamRes? FromTeam, TeamRes? ToTeam, PlayerInfoItemRes? Player);

/// <summary>Resolves transaction ids through <see cref="FantasyFootballClient.LeagueDirectory" /> and <see cref="FantasyFootballClient.PlayerCache" />.</summary>
public static class EspnLookups
{
    /// <summary>Copies each item on <paramref name="transaction" /> with its from team, to team, and player. Missing player cards are loaded once.</summary>
    public static async Task<IReadOnlyList<TransactionItemView>> ToItemViewsAsync(
        this TransactionRes transaction, FantasyFootballClient client, int leagueId, int seasonId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(transaction);
        ArgumentHelpers.ThrowIfNull(client);
        var items = transaction.Items;
        var playerIds = items.Where(item => item.PlayerId != null).Select(item => item.PlayerId!.Value).ToArray();
        var players = await LoadPlayersAsync(client, leagueId, seasonId, client.LeagueDirectory, playerIds, ct).ConfigureAwait(false);
        var index = client.LeagueDirectory;
        var views = new List<TransactionItemView>(items.Count);
        foreach (var item in items) {
            var player = item.PlayerId != null && players.TryGetValue(item.PlayerId.Value, out var card) ? card : null;
            views.Add(new(item, TeamOrNull(index, item.FromTeamId), TeamOrNull(index, item.ToTeamId), player));
        }

        return views;
    }

    private static async Task<IReadOnlyDictionary<int, PlayerInfoItemRes>> LoadPlayersAsync(
        FantasyFootballClient client, int leagueId, int seasonId, LeagueIndex? index, IReadOnlyList<int> playerIds, CancellationToken ct)
    {
        if (playerIds.Count == 0)
            return new Dictionary<int, PlayerInfoItemRes>();

        var period = index?.ScoringPeriodId is > 0 ? index.ScoringPeriodId : 18;
        var loaded = await client.Players.GetAsync(leagueId, seasonId, playerIds, period, ct).ConfigureAwait(false);
        return loaded.ToDictionary(player => player.Id);
    }

    private static TeamRes? TeamOrNull(LeagueIndex? index, int? teamId) => teamId != null ? index?.Team(teamId.Value) : null;
}
