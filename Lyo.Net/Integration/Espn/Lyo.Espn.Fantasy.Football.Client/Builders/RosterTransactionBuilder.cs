using Lyo.Espn.Fantasy.Football.Client.Models.Request;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football.Client.Builders;

/// <summary>Builds a free-agent add/drop, a waiver claim, or a waiver cancellation.</summary>
public sealed class RosterTransactionBuilder
{
    private readonly int? _bidAmount;
    private readonly string _executionType;
    private readonly List<FantasyTransactionItemReq> _items = [];
    private readonly string? _relatedTransactionId;
    private readonly int _scoringPeriodId;
    private readonly int _teamId;
    private readonly string _type;

    private RosterTransactionBuilder(int teamId, int scoringPeriodId, string type, int? bidAmount, string executionType, string? relatedTransactionId)
    {
        _teamId = teamId;
        _scoringPeriodId = scoringPeriodId;
        _type = type;
        _bidAmount = bidAmount;
        _executionType = executionType;
        _relatedTransactionId = relatedTransactionId;
    }

    /// <summary>Starts a free-agent add or drop. <see cref="Build" /> omits <c>bidAmount</c>.</summary>
    public static RosterTransactionBuilder FreeAgent(int teamId, int scoringPeriodId) => new(teamId, scoringPeriodId, "FREEAGENT", null, "EXECUTE", null);

    /// <summary>Starts a waiver claim. <see cref="Build" /> includes <paramref name="bid" />.</summary>
    public static RosterTransactionBuilder Waiver(int teamId, int scoringPeriodId, int bid) => new(teamId, scoringPeriodId, "WAIVER", bid, "EXECUTE", null);

    /// <summary>Starts a waiver cancellation. <see cref="Build" /> sets <c>executionType</c> to <c>CANCEL</c> and omits items.</summary>
    public static RosterTransactionBuilder CancelWaiver(int teamId, int scoringPeriodId, string relatedTransactionId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(relatedTransactionId);
        return new(teamId, scoringPeriodId, "WAIVER", null, "CANCEL", relatedTransactionId);
    }

    /// <summary>Adds a player onto the team.</summary>
    public RosterTransactionBuilder Add(int playerId)
    {
        _items.Add(new() { PlayerId = playerId, Type = "ADD", ToTeamId = _teamId });
        return this;
    }

    /// <summary>Drops a player from the team.</summary>
    public RosterTransactionBuilder Drop(int playerId)
    {
        _items.Add(new() { PlayerId = playerId, Type = "DROP", FromTeamId = _teamId });
        return this;
    }

    /// <summary>Returns the transaction body. An add/drop with no players throws. A cancellation has no items.</summary>
    public FantasyTransactionReq Build()
    {
        if (_executionType == "CANCEL") {
            return new() {
                IsLeagueManager = false,
                TeamId = _teamId,
                Type = _type,
                ScoringPeriodId = _scoringPeriodId,
                ExecutionType = _executionType,
                RelatedTransactionId = _relatedTransactionId
            };
        }

        OperationHelpers.ThrowIf(_items.Count == 0, "A roster transaction needs at least one add or drop.");
        return new() {
            IsLeagueManager = false,
            TeamId = _teamId,
            Type = _type,
            ScoringPeriodId = _scoringPeriodId,
            ExecutionType = _executionType,
            Items = _items.ToArray(),
            BidAmount = _bidAmount
        };
    }
}