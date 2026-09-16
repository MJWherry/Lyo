namespace Lyo.Leaderboard.Enums;

/// <summary>How an invite is meant to be delivered.</summary>
public enum InviteShareKind
{
    /// <summary>Copyable join link (unlimited uses until expiry).</summary>
    Link = 0,

    /// <summary>QR code over a short URL (unlimited uses until expiry).</summary>
    Qr = 1,

    /// <summary>Personal invite locked to an email address.</summary>
    Email = 2,

    /// <summary>Invite a known <c>LyoUser</c>.</summary>
    User = 3
}
