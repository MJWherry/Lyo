namespace Lyo.Espn.Fantasy.Football.Client.Models.Request;

/// <summary>Body posted to the fantasy writes host. Null fields are omitted.</summary>
public sealed record FantasyTransactionReq
{
    /// <summary>True when the acting user is posting as the league manager. Builders default this to false.</summary>
    public bool IsLeagueManager { get; init; }

    /// <summary>Team the transaction is filed for.</summary>
    public int TeamId { get; init; }

    /// <summary>ESPN transaction type, such as <c>ROSTER</c>, <c>FREEAGENT</c>, <c>WAIVER</c>, or <c>TRADE_PROPOSAL</c>.</summary>
    public string Type { get; init; } = "";

    /// <summary>Scoring period the transaction applies to.</summary>
    public int? ScoringPeriodId { get; init; }

    /// <summary><c>EXECUTE</c> for a new transaction, or <c>CANCEL</c> when withdrawing a waiver.</summary>
    public string? ExecutionType { get; init; }

    /// <summary>Acting member. The client fills this from <see cref="FantasyFootballClientOptions.Swid" /> when the builder left it empty.</summary>
    public string? MemberId { get; init; }

    /// <summary>Players to add, drop, move, or trade. Omitted when cancelling a waiver or responding to a trade.</summary>
    public IReadOnlyList<FantasyTransactionItemReq>? Items { get; init; }

    /// <summary>Waiver bid. Present only on waiver claims.</summary>
    public int? BidAmount { get; init; }

    /// <summary>Existing transaction a cancel or trade response refers to.</summary>
    public string? RelatedTransactionId { get; init; }

    /// <summary>Optional note. Trade declines and vetoes send this when the builder set it.</summary>
    public string? Comment { get; init; }

    /// <summary>Trade expiration as Unix milliseconds.</summary>
    public long? ExpirationDate { get; init; }
}

/// <summary>One player action inside a <see cref="FantasyTransactionReq" />.</summary>
public sealed record FantasyTransactionItemReq
{
    /// <summary>ESPN player id.</summary>
    public int PlayerId { get; init; }

    /// <summary><c>ADD</c>, <c>DROP</c>, <c>LINEUP</c>, or <c>TRADE</c>.</summary>
    public string Type { get; init; } = "";

    /// <summary>Team the player leaves. Used by drops and trades.</summary>
    public int? FromTeamId { get; init; }

    /// <summary>Team the player joins. Used by adds and trades.</summary>
    public int? ToTeamId { get; init; }

    /// <summary>Lineup slot the player is moving out of.</summary>
    public int? FromLineupSlotId { get; init; }

    /// <summary>Lineup slot the player is moving into.</summary>
    public int? ToLineupSlotId { get; init; }
}
