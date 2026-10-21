namespace Lyo.Leaderboard;

/// <summary>Materialized rank for one member on a board.</summary>
public sealed class StandingRecord
{
    /// <summary>Board.</summary>
    public Guid BoardId { get; set; }

    /// <summary>Member.</summary>
    public Guid MemberId { get; set; }

    /// <summary>Aggregated approved entry score.</summary>
    public decimal EntryScore { get; set; }

    /// <summary>Sum of live adjustments.</summary>
    public decimal AdjustmentScore { get; set; }

    /// <summary><see cref="EntryScore" /> plus <see cref="AdjustmentScore" />.</summary>
    public decimal TotalScore { get; set; }

    /// <summary>Approved entry count.</summary>
    public int EntryCount { get; set; }

    /// <summary>Most recent approved <c>occurred_at</c>.</summary>
    public DateTime? LastEntryAt { get; set; }

    /// <summary>1-based rank on the board.</summary>
    public int Rank { get; set; }

    /// <summary>When this row was last computed (UTC).</summary>
    public DateTime ComputedAt { get; set; }

    /// <summary>Member display name snapshot for API responses.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Linked user id, if the member has redeemed.</summary>
    public Guid? UserId { get; set; }
}