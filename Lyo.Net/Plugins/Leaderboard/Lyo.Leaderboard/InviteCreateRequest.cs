using Lyo.Leaderboard.Enums;

namespace Lyo.Leaderboard;

/// <summary>Input for <see cref="ILeaderboardStore.CreateInviteAsync" />.</summary>
public sealed class InviteCreateRequest
{
    /// <summary>Share channel.</summary>
    public InviteShareKind ShareKind { get; set; } = InviteShareKind.Link;

    /// <summary>Role granted on redeem.</summary>
    public MemberRole Role { get; set; } = MemberRole.Contributor;

    /// <summary>Required when <see cref="ShareKind" /> is <see cref="InviteShareKind.Email" />.</summary>
    public string? Email { get; set; }

    /// <summary>Optional SMS target.</summary>
    public string? Phone { get; set; }

    /// <summary>Required when <see cref="ShareKind" /> is <see cref="InviteShareKind.User" />.</summary>
    public Guid? InvitedUserId { get; set; }

    /// <summary>Optional display name snapshot for the pending member.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Optional expiry (UTC).</summary>
    public DateTime? ExpiresAt { get; set; }
}
