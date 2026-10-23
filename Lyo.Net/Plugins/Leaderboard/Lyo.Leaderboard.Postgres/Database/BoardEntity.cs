using System.Diagnostics;

namespace Lyo.Leaderboard.Postgres.Database;

/// <summary>Persisted board row.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BoardEntity
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid OwnerUserId { get; set; }

    public Guid TenantId { get; set; }

    public string Visibility { get; set; } = "private";

    public string JoinPolicy { get; set; } = "inviteOnly";

    public bool MembersCanSubmit { get; set; } = true;

    public bool RequireVerification { get; set; }

    public bool MembersCanEditOwn { get; set; } = true;

    public bool MembersCanDeleteOwn { get; set; }

    public bool AllowMultipleEntries { get; set; } = true;

    public string AggregationKind { get; set; } = "sum";

    public int? AggregationN { get; set; }

    public string SortDirection { get; set; } = "desc";

    public string TieBreak { get; set; } = "firstSubmitted";

    public string? TieBreakPropertyKey { get; set; }

    public string AlgorithmKey { get; set; } = string.Empty;

    public DateTime? StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<ScoringPropertyEntity> Properties { get; set; } = [];

    public List<MemberEntity> Members { get; set; } = [];

    public List<InviteEntity> Invites { get; set; } = [];

    public List<EntryEntity> Entries { get; set; } = [];

    public List<AdjustmentEntity> Adjustments { get; set; } = [];

    public List<StandingEntity> Standings { get; set; } = [];

    /// <summary>Type name, id, and name.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Name}";
}