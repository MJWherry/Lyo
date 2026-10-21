namespace Lyo.Leaderboard.Postgres.Database;

/// <summary>Persisted scored event.</summary>
public sealed class EntryEntity
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Guid MemberId { get; set; }

    public Guid SubmittedByUserId { get; set; }

    public string? Title { get; set; }

    public string? Notes { get; set; }

    public DateTime OccurredAt { get; set; }

    public string Status { get; set; } = "draft";

    public Guid? VerifiedByUserId { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? RejectionReason { get; set; }

    public decimal ComputedScore { get; set; }

    public string? ScoreBreakdownJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public BoardEntity? Board { get; set; }

    public MemberEntity? Member { get; set; }

    public List<EntryValueEntity> Values { get; set; } = [];
}