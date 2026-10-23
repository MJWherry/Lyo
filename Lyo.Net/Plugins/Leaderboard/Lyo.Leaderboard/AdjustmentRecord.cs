namespace Lyo.Leaderboard;

/// <summary>Manual point ledger line. Soft-delete is undo.</summary>
public sealed class AdjustmentRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning board.</summary>
    public Guid BoardId { get; set; }

    /// <summary>Member receiving the delta.</summary>
    public Guid MemberId { get; set; }

    /// <summary>Optional related entry.</summary>
    public Guid? EntryId { get; set; }

    /// <summary>Points added (negative to subtract).</summary>
    public decimal Delta { get; set; }

    /// <summary>Why the adjustment was made.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Actor who created the line.</summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>Creation time (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the line was undone.</summary>
    public DateTime? DeletedAt { get; set; }
}