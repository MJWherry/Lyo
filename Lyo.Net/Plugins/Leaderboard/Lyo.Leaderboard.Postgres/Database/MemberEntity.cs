using System.Diagnostics;

namespace Lyo.Leaderboard.Postgres.Database;

/// <summary>Persisted membership.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MemberEntity
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public Guid? UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = "contributor";

    public string Status { get; set; } = "invited";

    public Guid? InvitedByUserId { get; set; }

    public DateTime? JoinedAt { get; set; }

    public DateTime? RemovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public BoardEntity? Board { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}