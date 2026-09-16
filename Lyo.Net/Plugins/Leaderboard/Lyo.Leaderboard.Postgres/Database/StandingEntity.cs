namespace Lyo.Leaderboard.Postgres.Database;

/// <summary>Materialized standing row.</summary>
public sealed class StandingEntity
{
    public Guid BoardId { get; set; }

    public Guid MemberId { get; set; }

    public decimal EntryScore { get; set; }

    public decimal AdjustmentScore { get; set; }

    public decimal TotalScore { get; set; }

    public int EntryCount { get; set; }

    public DateTime? LastEntryAt { get; set; }

    public int Rank { get; set; }

    public DateTime ComputedAt { get; set; }

    public BoardEntity? Board { get; set; }

    public MemberEntity? Member { get; set; }
}
