using Lyo.Leaderboard.Enums;

namespace Lyo.Leaderboard;

/// <summary>One scored event (a catch, a round, a chore).</summary>
public sealed class EntryRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning board.</summary>
    public Guid BoardId { get; set; }

    /// <summary>Member this entry counts for.</summary>
    public Guid MemberId { get; set; }

    /// <summary>User who submitted (may be an admin entering on behalf of the member).</summary>
    public Guid SubmittedByUserId { get; set; }

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Optional notes.</summary>
    public string? Notes { get; set; }

    /// <summary>When the event happened (UTC).</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>Verification status.</summary>
    public EntryStatus Status { get; set; } = EntryStatus.Draft;

    /// <summary>Who verified or rejected.</summary>
    public Guid? VerifiedByUserId { get; set; }

    /// <summary>When it was verified or rejected.</summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>Reason when rejected.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>Cached total from the scoring engine.</summary>
    public decimal ComputedScore { get; set; }

    /// <summary>Per-property breakdown JSON (serialized form).</summary>
    public string? ScoreBreakdownJson { get; set; }

    /// <summary>Typed property values.</summary>
    public List<EntryValueRecord> Values { get; set; } = [];

    /// <summary>Creation time (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last update time (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft-delete timestamp.</summary>
    public DateTime? DeletedAt { get; set; }
}