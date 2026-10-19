namespace Lyo.FantasyFootball;

/// <summary>One league season. <see cref="EspnLeagueId" /> and <see cref="SeasonId" /> are the ESPN pair a refresh upserts.</summary>
public sealed class FantasyLeagueRecord
{
    /// <summary>Store id. Empty until the first save.</summary>
    public Guid Id { get; set; }

    /// <summary>ESPN league id.</summary>
    public int EspnLeagueId { get; set; }

    /// <summary>ESPN season, such as 2026.</summary>
    public int SeasonId { get; set; }

    /// <summary>League name from ESPN settings, when the sync had one.</summary>
    public string? Name { get; set; }

    /// <summary>Current scoring period. Roster rows use this period.</summary>
    public int ScoringPeriodId { get; set; }

    /// <summary>Members in the league.</summary>
    public IReadOnlyList<FantasyMemberRecord> Members { get; set; } = [];

    /// <summary>Teams and the roster for <see cref="ScoringPeriodId" />.</summary>
    public IReadOnlyList<FantasyTeamRecord> Teams { get; set; } = [];
}

/// <summary>A league member. <see cref="EspnMemberId" /> is the ESPN SWID.</summary>
public sealed class FantasyMemberRecord
{
    /// <summary>Store id. Empty until the first save.</summary>
    public Guid Id { get; set; }

    /// <summary>ESPN member id (SWID).</summary>
    public string EspnMemberId { get; set; } = "";

    /// <summary>Name ESPN shows in the league.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Given name, when ESPN sent one.</summary>
    public string? FirstName { get; set; }

    /// <summary>Family name, when ESPN sent one.</summary>
    public string? LastName { get; set; }
}

/// <summary>A fantasy team and its roster for one scoring period.</summary>
public sealed class FantasyTeamRecord
{
    /// <summary>Store id. Empty until the first save.</summary>
    public Guid Id { get; set; }

    /// <summary>ESPN team id.</summary>
    public int EspnTeamId { get; set; }

    /// <summary>Team name.</summary>
    public string? Name { get; set; }

    /// <summary>Short name.</summary>
    public string? Abbrev { get; set; }

    /// <summary>Location half of the ESPN name.</summary>
    public string? Location { get; set; }

    /// <summary>Nickname half of the ESPN name.</summary>
    public string? Nickname { get; set; }

    /// <summary>Primary owner SWID.</summary>
    public string? PrimaryOwnerId { get; set; }

    /// <summary>Players on the roster for the league scoring period.</summary>
    public IReadOnlyList<FantasyRosterEntryRecord> Roster { get; set; } = [];
}

/// <summary>One player on a team for a scoring period.</summary>
public sealed class FantasyRosterEntryRecord
{
    /// <summary>Scoring period this row belongs to.</summary>
    public int ScoringPeriodId { get; set; }

    /// <summary>ESPN player id.</summary>
    public int PlayerId { get; set; }

    /// <summary>Player name captured at sync time.</summary>
    public string? PlayerName { get; set; }
}

/// <summary>One Fantasy Chat topic: the league chat or a direct message.</summary>
public sealed class FantasyConversationRecord
{
    /// <summary>Store id. Empty until the first save.</summary>
    public Guid Id { get; set; }

    /// <summary>ESPN topic id.</summary>
    public string EspnTopicId { get; set; } = "";

    /// <summary>ESPN topic type, such as <c>CHAT_ALL_MEMBERS</c> or <c>CHAT_DIRECT_MESSAGE</c>.</summary>
    public string? Type { get; set; }

    /// <summary>Unread count from the last sync.</summary>
    public int UnreadCount { get; set; }

    /// <summary>Messages ESPN says are in the topic, including ones outside the stored page.</summary>
    public int TotalMessageCount { get; set; }

    /// <summary>Topic subject, when ESPN sent one.</summary>
    public string? Subject { get; set; }

    /// <summary>Members who can see the topic.</summary>
    public IReadOnlyList<FantasyMemberRecord> Members { get; set; } = [];
}

/// <summary>One chat message.</summary>
public sealed class FantasyMessageRecord
{
    /// <summary>Store id. Empty until the first save.</summary>
    public Guid Id { get; set; }

    /// <summary>ESPN message id.</summary>
    public string EspnMessageId { get; set; } = "";

    /// <summary>Author SWID, or <c>LM</c> for the league manager.</summary>
    public string? AuthorEspnId { get; set; }

    /// <summary>Author display name when the league already had that member.</summary>
    public string? AuthorName { get; set; }

    /// <summary><c>Text</c>, <c>AddDrop</c>, <c>Trade</c>, or <c>Unknown</c>.</summary>
    public string Kind { get; set; } = "Text";

    /// <summary>Text body. Empty for add/drop and trade cards.</summary>
    public string? Content { get; set; }

    /// <summary>When ESPN says the message was sent.</summary>
    public DateTimeOffset SentAt { get; set; }

    /// <summary>ESPN transaction id for a trade card.</summary>
    public string? EspnTransactionId { get; set; }

    /// <summary>Players added or dropped on each side.</summary>
    public IReadOnlyList<FantasyMessagePlayerRecord> Players { get; set; } = [];
}

/// <summary>One player on a chat add/drop or trade.</summary>
public sealed class FantasyMessagePlayerRecord
{
    /// <summary>1 or 2. A trade uses both. An add/drop uses side 1.</summary>
    public int Side { get; set; }

    /// <summary>ESPN team id for this side.</summary>
    public int? EspnTeamId { get; set; }

    /// <summary>ESPN player id.</summary>
    public int PlayerId { get; set; }

    /// <summary>Player name captured at sync time.</summary>
    public string? PlayerName { get; set; }

    /// <summary><c>added</c> or <c>dropped</c>.</summary>
    public string Direction { get; set; } = "";
}

/// <summary>One trade proposal or response.</summary>
public sealed class FantasyTradeRecord
{
    /// <summary>Store id. Empty until the first save.</summary>
    public Guid Id { get; set; }

    /// <summary>ESPN transaction id.</summary>
    public string EspnTransactionId { get; set; } = "";

    /// <summary>ESPN type, such as <c>TRADE_PROPOSAL</c> or <c>TRADE_ACCEPT</c>.</summary>
    public string? Type { get; set; }

    /// <summary>ESPN status.</summary>
    public string? Status { get; set; }

    /// <summary>Scoring period, when ESPN sent one.</summary>
    public int? ScoringPeriodId { get; set; }

    /// <summary>True while the proposal is still open.</summary>
    public bool IsPending { get; set; }

    /// <summary>Note sent with the proposal or the response.</summary>
    public string? Comment { get; set; }

    /// <summary>Players moving between teams.</summary>
    public IReadOnlyList<FantasyTradeItemRecord> Items { get; set; } = [];
}

/// <summary>One player on a trade.</summary>
public sealed class FantasyTradeItemRecord
{
    /// <summary>Team giving the player.</summary>
    public int? FromEspnTeamId { get; set; }

    /// <summary>Team receiving the player.</summary>
    public int? ToEspnTeamId { get; set; }

    /// <summary>ESPN player id.</summary>
    public int? PlayerId { get; set; }

    /// <summary>Player name captured at sync time.</summary>
    public string? PlayerName { get; set; }
}
