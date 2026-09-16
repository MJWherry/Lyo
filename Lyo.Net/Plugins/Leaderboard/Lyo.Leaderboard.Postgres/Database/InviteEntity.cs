namespace Lyo.Leaderboard.Postgres.Database;

/// <summary>Persisted invite.</summary>
public sealed class InviteEntity
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public string ShareKind { get; set; } = "link";

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public Guid? InvitedUserId { get; set; }

    public string? ShortUrlId { get; set; }

    public string Role { get; set; } = "contributor";

    public int MaxUses { get; set; }

    public int UseCount { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public BoardEntity? Board { get; set; }
}
