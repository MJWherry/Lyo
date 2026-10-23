using System.Diagnostics;

namespace Lyo.Leaderboard.Postgres.Database;

/// <summary>Persisted manual point adjustment.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AdjustmentEntity
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Guid MemberId { get; set; }

    public Guid? EntryId { get; set; }

    public decimal Delta { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public BoardEntity? Board { get; set; }

    public MemberEntity? Member { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}