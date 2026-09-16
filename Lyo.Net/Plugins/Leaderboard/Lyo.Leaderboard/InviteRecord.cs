using Lyo.Leaderboard.Enums;

namespace Lyo.Leaderboard;

/// <summary>Persisted invite (token hash only; plaintext is returned once at create).</summary>
public sealed class InviteRecord
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning board.</summary>
    public Guid BoardId { get; set; }

    /// <summary>SHA-256 hex of the plaintext token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>How the invite is shared.</summary>
    public InviteShareKind ShareKind { get; set; } = InviteShareKind.Link;

    /// <summary>Email lock for personal invites.</summary>
    public string? Email { get; set; }

    /// <summary>Optional phone for a later SMS send.</summary>
    public string? Phone { get; set; }

    /// <summary>Known user this invite targets.</summary>
    public Guid? InvitedUserId { get; set; }

    /// <summary>Id in <c>url.short_urls</c> (no cross-schema FK).</summary>
    public string? ShortUrlId { get; set; }

    /// <summary>Role granted on redeem. Owner cannot be granted this way.</summary>
    public MemberRole Role { get; set; } = MemberRole.Contributor;

    /// <summary>Maximum redemptions. <c>0</c> means unlimited.</summary>
    public int MaxUses { get; set; }

    /// <summary>Successful redemptions so far.</summary>
    public int UseCount { get; set; }

    /// <summary>When the invite expires (UTC).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>When the invite was revoked.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Creator <c>LyoUser</c> id.</summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>Creation time (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
