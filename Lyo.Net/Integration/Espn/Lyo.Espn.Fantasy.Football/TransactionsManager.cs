using Lyo.Espn.Fantasy.Football.Builders;
using Lyo.Espn.Fantasy.Football.Models.Request;
using Lyo.Espn.Fantasy.Football.Models.Response;

namespace Lyo.Espn.Fantasy.Football;

/// <summary>Transaction reads for league activity and trades.</summary>
public class TransactionsManager(FantasyFootballClient client)
{
    private static readonly string[] TradeTypes = ["TRADE_ACCEPT", "TRADE_PROPOSAL", "TRADE_VETO", "TRADE_UPHOLD", "TRADE_DECLINE", "TRADE_ERROR"];

    /// <summary>Gets recent transactions for the league.</summary>
    public async Task<IReadOnlyList<TransactionRes>?> GetRecentAsync(int leagueId, int seasonId, TransactionsQuery? query = null, CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetRecentAsync));
        query ??= new();
        var queryParams = new Dictionary<string, string?>();
        if (query.ScoringPeriodId != null)
            queryParams["scoringPeriodId"] = query.ScoringPeriodId.Value.ToString();

        var path = client.BuildLeaguePath(leagueId, seasonId, ["mTransactions2"], queryParams);
        FantasyFilterReq? filter = null;
        if (query.Types.Count > 0)
            filter = FantasyFilterReqBuilder.ForTransactions(query.Types).Build();

        var response = await client.GetLeagueViewAsync<TransactionsResponseRes>(path, filter, ct).ConfigureAwait(false);
        return response?.Transactions;
    }

    /// <summary>Gets recent trade-related transactions for the league.</summary>
    public async Task<IReadOnlyList<TransactionRes>?> GetRecentTradesAsync(int leagueId, int seasonId, int? scoringPeriodId = null, CancellationToken ct = default)
        => await GetRecentAsync(leagueId, seasonId, new() { ScoringPeriodId = scoringPeriodId, Types = TradeTypes }, ct).ConfigureAwait(false);
}