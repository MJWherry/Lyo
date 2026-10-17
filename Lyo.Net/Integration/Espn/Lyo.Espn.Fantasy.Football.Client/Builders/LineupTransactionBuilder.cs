using Lyo.Espn.Fantasy.Football.Client.Models.Request;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football.Client.Builders;

/// <summary>Builds a <c>ROSTER</c> transaction with one <c>LINEUP</c> item per move. Call <see cref="Build" /> and pass the result to the transactions manager.</summary>
public sealed class LineupTransactionBuilder
{
    private readonly int _teamId;
    private readonly int _scoringPeriodId;
    private readonly List<FantasyTransactionItemReq> _moves = [];

    /// <summary>Starts a lineup edit for one team and scoring period.</summary>
    public LineupTransactionBuilder(int teamId, int scoringPeriodId)
    {
        _teamId = teamId;
        _scoringPeriodId = scoringPeriodId;
    }

    /// <summary>Moves a player from one lineup slot to another.</summary>
    public LineupTransactionBuilder Move(int playerId, LineupSlot fromSlot, LineupSlot toSlot) => Move(playerId, (int)fromSlot, (int)toSlot);

    /// <summary>Moves a player from one lineup slot id to another.</summary>
    public LineupTransactionBuilder Move(int playerId, int fromSlotId, int toSlotId)
    {
        _moves.Add(new() { PlayerId = playerId, Type = "LINEUP", FromLineupSlotId = fromSlotId, ToLineupSlotId = toSlotId });
        return this;
    }

    /// <summary>Returns the roster transaction. Throws when no moves were added.</summary>
    public FantasyTransactionReq Build()
    {
        OperationHelpers.ThrowIf(_moves.Count == 0, "A lineup transaction needs at least one move.");
        return new() {
            IsLeagueManager = false,
            TeamId = _teamId,
            Type = "ROSTER",
            ScoringPeriodId = _scoringPeriodId,
            ExecutionType = "EXECUTE",
            Items = _moves.ToArray()
        };
    }
}
