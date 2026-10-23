using Lyo.Leaderboard.Enums;

namespace Lyo.Leaderboard;

/// <summary>A person on a board. <see cref="UserId" /> is null until an invite is redeemed.</summary>
public sealed class MemberRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning board.</summary>
    public Guid BoardId { get; set; }

    /// <summary>Linked <c>LyoUser</c> id, if known.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Display name snapshot.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Board role.</summary>
    public MemberRole Role { get; set; } = MemberRole.Contributor;

    /// <summary>Membership lifecycle.</summary>
    public MemberStatus Status { get; set; } = MemberStatus.Invited;

    /// <summary>Who issued the invite, if any.</summary>
    public Guid? InvitedByUserId { get; set; }

    /// <summary>When the member became active.</summary>
    public DateTime? JoinedAt { get; set; }

    /// <summary>When the member was removed.</summary>
    public DateTime? RemovedAt { get; set; }

    /// <summary>Creation time (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last update time (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }
}