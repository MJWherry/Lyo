using Lyo.Leaderboard.Enums;

namespace Lyo.Leaderboard.Api.Models;

/// <summary>Create an invite on a board.</summary>
public sealed class CreateInviteApiRequest
{
    /// <summary>Share channel.</summary>
    public InviteShareKind ShareKind { get; set; } = InviteShareKind.Link;

    /// <summary>Role granted on redeem.</summary>
    public MemberRole Role { get; set; } = MemberRole.Contributor;

    /// <summary>Email for personal invites.</summary>
    public string? Email { get; set; }

    /// <summary>Optional phone.</summary>
    public string? Phone { get; set; }

    /// <summary>Known user id.</summary>
    public Guid? InvitedUserId { get; set; }

    /// <summary>Display name snapshot.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Optional expiry.</summary>
    public DateTime? ExpiresAt { get; set; }
}
