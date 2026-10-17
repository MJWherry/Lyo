using Lyo.Espn.Fantasy.Football.Client.Builders;
using Lyo.Espn.Fantasy.Football.Client.Models.Request;
using Lyo.Espn.Fantasy.Football.Client.Models.Response;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>League activity and trade reads, plus lineup, roster, and trade writes.</summary>
public class TransactionsManager(FantasyFootballClient client)
{
    private static readonly string[] TradeTypes = ["TRADE_ACCEPT", "TRADE_PROPOSAL", "TRADE_VETO", "TRADE_UPHOLD", "TRADE_DECLINE", "TRADE_ERROR"];

    /// <summary>Loads recent transactions for the league.</summary>
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

    /// <summary>Loads recent trade-related transactions for the league.</summary>
    public async Task<IReadOnlyList<TransactionRes>?> GetRecentTradesAsync(int leagueId, int seasonId, int? scoringPeriodId = null, CancellationToken ct = default)
        => await GetRecentAsync(leagueId, seasonId, new() { ScoringPeriodId = scoringPeriodId, Types = TradeTypes }, ct).ConfigureAwait(false);

    /// <summary>Loads pending transactions from the <c>mPendingTransactions</c> view.</summary>
    public async Task<IReadOnlyList<TransactionRes>?> GetPendingAsync(int leagueId, int seasonId, CancellationToken ct = default)
    {
        var path = client.BuildLeaguePath(leagueId, seasonId, ["mPendingTransactions"]);
        var response = await client.GetLeagueViewAsync<TransactionsResponseRes>(path, ct: ct).ConfigureAwait(false);
        return response?.Transactions;
    }

    /// <summary>Posts a built transaction to the writes host. <see cref="FantasyFootballClientOptions.Swid" /> is sent as <c>memberId</c> when the body left it empty.</summary>
    public Task<TransactionRes> PostTransactionAsync(int leagueId, int seasonId, FantasyTransactionReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var memberId = string.IsNullOrWhiteSpace(request.MemberId) ? client.Swid : request.MemberId;
        var body = string.IsNullOrWhiteSpace(memberId) ? request : request with { MemberId = memberId };
        return client.PostAuthenticatedAsync<FantasyTransactionReq, TransactionRes>(client.BuildWritesPath(leagueId, seasonId), body, ct);
    }

    /// <summary>Posts the lineup built by <paramref name="lineup" />.</summary>
    public Task<TransactionRes> SetLineupAsync(int leagueId, int seasonId, LineupTransactionBuilder lineup, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(lineup);
        return PostTransactionAsync(leagueId, seasonId, lineup.Build(), ct);
    }

    /// <summary>Posts a free-agent or waiver add/drop built by <paramref name="roster" />.</summary>
    public Task<TransactionRes> AddDropAsync(int leagueId, int seasonId, RosterTransactionBuilder roster, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(roster);
        return PostTransactionAsync(leagueId, seasonId, roster.Build(), ct);
    }

    /// <summary>Posts a waiver cancellation built by <paramref name="waiver" />.</summary>
    public Task<TransactionRes> CancelWaiverAsync(int leagueId, int seasonId, RosterTransactionBuilder waiver, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(waiver);
        return PostTransactionAsync(leagueId, seasonId, waiver.Build(), ct);
    }

    /// <summary>Posts a trade proposal built by <paramref name="trade" />.</summary>
    public Task<TransactionRes> ProposeTradeAsync(int leagueId, int seasonId, TradeTransactionBuilder trade, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(trade);
        return PostTransactionAsync(leagueId, seasonId, trade.Build(), ct);
    }

    /// <summary>Posts an accept, decline, uphold, or veto built by <paramref name="response" />.</summary>
    public Task<TransactionRes> RespondToTradeAsync(int leagueId, int seasonId, TradeTransactionBuilder response, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(response);
        return PostTransactionAsync(leagueId, seasonId, response.Build(), ct);
    }
}
