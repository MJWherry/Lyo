using Lyo.Espn.Fantasy.Football.Client.Models.Request;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football.Client.Builders;

/// <summary>Builds a trade proposal or a response to an existing proposal.</summary>
public sealed class TradeTransactionBuilder
{
    private readonly int _teamId;
    private readonly int _otherTeamId;
    private readonly int _scoringPeriodId;
    private readonly string? _responseType;
    private readonly string? _relatedTransactionId;
    private readonly List<FantasyTransactionItemReq> _items = [];
    private string? _comment;
    private long? _expirationDate;

    /// <summary>Starts a proposal from <paramref name="teamId" /> to <paramref name="otherTeamId" />.</summary>
    public TradeTransactionBuilder(int teamId, int otherTeamId, int scoringPeriodId)
    {
        _teamId = teamId;
        _otherTeamId = otherTeamId;
        _scoringPeriodId = scoringPeriodId;
    }

    private TradeTransactionBuilder(int teamId, int scoringPeriodId, string relatedTransactionId, string responseType)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(relatedTransactionId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(responseType);
        _teamId = teamId;
        _scoringPeriodId = scoringPeriodId;
        _relatedTransactionId = relatedTransactionId;
        _responseType = responseType;
    }

    /// <summary>Offers a player from this team to the other team.</summary>
    public TradeTransactionBuilder Send(int playerId)
    {
        _items.Add(new() { PlayerId = playerId, Type = "TRADE", FromTeamId = _teamId, ToTeamId = _otherTeamId });
        return this;
    }

    /// <summary>Asks for a player from the other team.</summary>
    public TradeTransactionBuilder Receive(int playerId)
    {
        _items.Add(new() { PlayerId = playerId, Type = "TRADE", FromTeamId = _otherTeamId, ToTeamId = _teamId });
        return this;
    }

    /// <summary>Attaches a note. Blank text is omitted. Declines and vetoes send this when it is set.</summary>
    public TradeTransactionBuilder Comment(string? comment)
    {
        _comment = string.IsNullOrWhiteSpace(comment) ? null : comment;
        return this;
    }

    /// <summary>Sets the proposal expiration as Unix milliseconds.</summary>
    public TradeTransactionBuilder ExpiresAt(DateTimeOffset expiresAt)
    {
        _expirationDate = ToUnixMilliseconds(expiresAt);
        return this;
    }

    /// <summary>Accepts an existing proposal.</summary>
    public static TradeTransactionBuilder Accept(int teamId, int scoringPeriodId, string relatedTransactionId)
        => new(teamId, scoringPeriodId, relatedTransactionId, "TRADE_ACCEPT");

    /// <summary>Declines an existing proposal. Use <see cref="Comment" /> when a reason should be sent.</summary>
    public static TradeTransactionBuilder Decline(int teamId, int scoringPeriodId, string relatedTransactionId)
        => new(teamId, scoringPeriodId, relatedTransactionId, "TRADE_DECLINE");

    /// <summary>Upholds an existing proposal.</summary>
    public static TradeTransactionBuilder Uphold(int teamId, int scoringPeriodId, string relatedTransactionId)
        => new(teamId, scoringPeriodId, relatedTransactionId, "TRADE_UPHOLD");

    /// <summary>Vetoes an existing proposal. Use <see cref="Comment" /> when a reason should be sent.</summary>
    public static TradeTransactionBuilder Veto(int teamId, int scoringPeriodId, string relatedTransactionId)
        => new(teamId, scoringPeriodId, relatedTransactionId, "TRADE_VETO");

    /// <summary>Returns the trade body. A proposal with no players throws. A response carries <c>relatedTransactionId</c> and no items.</summary>
    public FantasyTransactionReq Build()
    {
        if (_responseType != null) {
            return new() {
                IsLeagueManager = false,
                TeamId = _teamId,
                Type = _responseType,
                ScoringPeriodId = _scoringPeriodId,
                ExecutionType = "EXECUTE",
                RelatedTransactionId = _relatedTransactionId,
                Comment = _comment
            };
        }

        OperationHelpers.ThrowIf(_items.Count == 0, "A trade proposal needs at least one player.");
        return new() {
            IsLeagueManager = false,
            TeamId = _teamId,
            Type = "TRADE_PROPOSAL",
            ScoringPeriodId = _scoringPeriodId,
            ExecutionType = "EXECUTE",
            Items = _items.ToArray(),
            Comment = _comment,
            ExpirationDate = _expirationDate
        };
    }

    private static long ToUnixMilliseconds(DateTimeOffset value)
        => (value.ToUniversalTime().Ticks - 621355968000000000L) / TimeSpan.TicksPerMillisecond;
}
