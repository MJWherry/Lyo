namespace Lyo.FantasyFootball.Postgres.Database;

/// <summary>League row. Unique on ESPN league id and season.</summary>
public sealed class LeagueEntity
{
    public Guid Id { get; set; }

    public int EspnLeagueId { get; set; }

    public int SeasonId { get; set; }

    public string? Name { get; set; }

    public int ScoringPeriodId { get; set; }

    public List<MemberEntity> Members { get; set; } = [];

    public List<TeamEntity> Teams { get; set; } = [];

    public List<ConversationEntity> Conversations { get; set; } = [];

    public List<TradeEntity> Trades { get; set; } = [];
}

/// <summary>Member row. Unique on league and SWID.</summary>
public sealed class MemberEntity
{
    public Guid Id { get; set; }

    public Guid LeagueId { get; set; }

    public LeagueEntity League { get; set; } = null!;

    public string EspnMemberId { get; set; } = "";

    public string? DisplayName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}

/// <summary>Team row. Unique on league and ESPN team id.</summary>
public sealed class TeamEntity
{
    public Guid Id { get; set; }

    public Guid LeagueId { get; set; }

    public LeagueEntity League { get; set; } = null!;

    public int EspnTeamId { get; set; }

    public string? Name { get; set; }

    public string? Abbrev { get; set; }

    public string? Location { get; set; }

    public string? Nickname { get; set; }

    public string? PrimaryOwnerId { get; set; }

    public List<RosterEntryEntity> Roster { get; set; } = [];
}

/// <summary>One roster player for a scoring period.</summary>
public sealed class RosterEntryEntity
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public TeamEntity Team { get; set; } = null!;

    public int ScoringPeriodId { get; set; }

    public int PlayerId { get; set; }

    public string? PlayerName { get; set; }
}

/// <summary>Chat topic. Unique on league and ESPN topic id.</summary>
public sealed class ConversationEntity
{
    public Guid Id { get; set; }

    public Guid LeagueId { get; set; }

    public LeagueEntity League { get; set; } = null!;

    public string EspnTopicId { get; set; } = "";

    public string? Type { get; set; }

    public int UnreadCount { get; set; }

    public int TotalMessageCount { get; set; }

    public string? Subject { get; set; }

    public List<ConversationMemberEntity> Members { get; set; } = [];

    public List<MessageEntity> Messages { get; set; } = [];
}

/// <summary>Member who can see a topic.</summary>
public sealed class ConversationMemberEntity
{
    public Guid ConversationId { get; set; }

    public ConversationEntity Conversation { get; set; } = null!;

    public Guid MemberId { get; set; }

    public MemberEntity Member { get; set; } = null!;
}

/// <summary>Chat message. Unique on topic and ESPN message id.</summary>
public sealed class MessageEntity
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public ConversationEntity Conversation { get; set; } = null!;

    public string EspnMessageId { get; set; } = "";

    public string? AuthorEspnId { get; set; }

    public string? AuthorName { get; set; }

    public string Kind { get; set; } = "Text";

    public string? Content { get; set; }

    public DateTimeOffset SentAt { get; set; }

    public string? EspnTransactionId { get; set; }

    public List<MessagePlayerEntity> Players { get; set; } = [];
}

/// <summary>A player added or dropped on a message.</summary>
public sealed class MessagePlayerEntity
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public MessageEntity Message { get; set; } = null!;

    public int Side { get; set; }

    public int? EspnTeamId { get; set; }

    public int PlayerId { get; set; }

    public string? PlayerName { get; set; }

    public string Direction { get; set; } = "";
}

/// <summary>Trade row. Unique on league and ESPN transaction id.</summary>
public sealed class TradeEntity
{
    public Guid Id { get; set; }

    public Guid LeagueId { get; set; }

    public LeagueEntity League { get; set; } = null!;

    public string EspnTransactionId { get; set; } = "";

    public string? Type { get; set; }

    public string? Status { get; set; }

    public int? ScoringPeriodId { get; set; }

    public bool IsPending { get; set; }

    public string? Comment { get; set; }

    public List<TradeItemEntity> Items { get; set; } = [];
}

/// <summary>One player moving on a trade.</summary>
public sealed class TradeItemEntity
{
    public Guid Id { get; set; }

    public Guid TradeId { get; set; }

    public TradeEntity Trade { get; set; } = null!;

    public int? FromEspnTeamId { get; set; }

    public int? ToEspnTeamId { get; set; }

    public int? PlayerId { get; set; }

    public string? PlayerName { get; set; }
}
