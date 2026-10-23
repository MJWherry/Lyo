using Lyo.Common.Core.Enums;
using Lyo.Leaderboard.Enums;

namespace Lyo.Leaderboard;

/// <summary>A user-created game with its own scoring schema and membership.</summary>
public sealed class BoardRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>URL-safe unique slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Owning <c>LyoUser</c> id.</summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>Workspace / tenant scope. Defaults to the owner id.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Who can discover the board.</summary>
    public BoardVisibility Visibility { get; set; } = BoardVisibility.Private;

    /// <summary>How people join.</summary>
    public JoinPolicy JoinPolicy { get; set; } = JoinPolicy.InviteOnly;

    /// <summary>When false, only owner/admin may create entries.</summary>
    public bool MembersCanSubmit { get; set; } = true;

    /// <summary>When true, contributor entries start as <see cref="EntryStatus.Pending" />.</summary>
    public bool RequireVerification { get; set; }

    /// <summary>Contributors may edit their own draft/pending entries.</summary>
    public bool MembersCanEditOwn { get; set; } = true;

    /// <summary>Contributors may delete their own draft/pending entries.</summary>
    public bool MembersCanDeleteOwn { get; set; }

    /// <summary>When false, a member may have at most one non-deleted entry.</summary>
    public bool AllowMultipleEntries { get; set; } = true;

    /// <summary>How approved entries collapse into a standing.</summary>
    public AggregationKind AggregationKind { get; set; } = AggregationKind.Sum;

    /// <summary>N for <see cref="AggregationKind.BestN" />.</summary>
    public int? AggregationN { get; set; }

    /// <summary>High score wins (<see cref="Common.Core.Enums.SortDirection.Desc" />) or low score wins.</summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;

    /// <summary>How equal totals are ordered.</summary>
    public TieBreakKind TieBreak { get; set; } = TieBreakKind.FirstSubmitted;

    /// <summary>Property key used when <see cref="TieBreak" /> is <see cref="TieBreakKind.NamedProperty" />.</summary>
    public string? TieBreakPropertyKey { get; set; }

    /// <summary>Catalog key of the board expression. Set when the board is created.</summary>
    public string AlgorithmKey { get; set; } = string.Empty;

    /// <summary>Expression source for the board score. Stored on the algorithm revision, not on the board row.</summary>
    public string? ScoreFormula { get; set; }

    /// <summary>Optional window start (UTC). Submissions before this are rejected.</summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>Optional window end (UTC). Submissions after this are rejected.</summary>
    public DateTime? EndsAt { get; set; }

    /// <summary>When the board was archived.</summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>Soft-delete timestamp.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Creation time (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last update time (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }
}