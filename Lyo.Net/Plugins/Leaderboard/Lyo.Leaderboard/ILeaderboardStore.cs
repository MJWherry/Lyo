namespace Lyo.Leaderboard;

/// <summary>Persistence and policy for boards, scoring properties, members, invites, entries, and standings.</summary>
public interface ILeaderboardStore
{
    /// <summary>Creates or updates a board. A new board also inserts an owner membership for <paramref name="actor" />.</summary>
    Task<BoardRecord> SaveBoardAsync(BoardRecord board, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Gets a board by id. Returns null when missing or not visible to <paramref name="actor" />.</summary>
    Task<BoardRecord?> GetBoardByIdAsync(Guid id, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Gets a board by slug. Returns null when missing or not visible to <paramref name="actor" />.</summary>
    Task<BoardRecord?> GetBoardBySlugAsync(string slug, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Archives a board. Owner only.</summary>
    Task ArchiveBoardAsync(Guid boardId, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Creates or updates a scoring property. Owner only. Key/kind changes are rejected after approved entries exist.</summary>
    Task<ScoringPropertyRecord> SavePropertyAsync(ScoringPropertyRecord property, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Deletes a scoring property. Owner only. Rejected after approved entries exist.</summary>
    Task DeletePropertyAsync(Guid propertyId, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Lists properties for a board the actor can see.</summary>
    Task<IReadOnlyList<ScoringPropertyRecord>> ListPropertiesAsync(Guid boardId, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Updates a member role or status. Owner/admin; owner role cannot be granted or removed this way.</summary>
    Task<MemberRecord> SaveMemberAsync(MemberRecord member, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Removes a member. Owner/admin.</summary>
    Task RemoveMemberAsync(Guid memberId, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Lists members for a board the actor can see.</summary>
    Task<IReadOnlyList<MemberRecord>> ListMembersAsync(Guid boardId, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Creates an invite and returns the plaintext token once.</summary>
    Task<InviteCreateResult> CreateInviteAsync(Guid boardId, InviteCreateRequest request, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Stores the short-url id produced by the host shortener.</summary>
    Task AttachShortUrlAsync(Guid inviteId, string shortUrlId, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Gets an invite the actor can administer, or any invite by id for QR generation after create.</summary>
    Task<InviteRecord?> GetInviteByIdAsync(Guid inviteId, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Redeems a plaintext token for <paramref name="actor" />.</summary>
    Task<MemberRecord> RedeemInviteAsync(string token, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Revokes an invite. Owner/admin.</summary>
    Task RevokeInviteAsync(Guid inviteId, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Creates or updates an entry, validates values, scores, and refreshes standings when approved.</summary>
    Task<EntryRecord> SaveEntryAsync(EntryRecord entry, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Gets an entry the actor can see.</summary>
    Task<EntryRecord?> GetEntryByIdAsync(Guid entryId, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Lists entries on a visible board. Non-admins only see approved rows plus their own submissions.</summary>
    Task<IReadOnlyList<EntryRecord>> ListEntriesAsync(Guid boardId, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Changes entry status (verify / reject / withdraw).</summary>
    Task<EntryRecord> SetEntryStatusAsync(Guid entryId, EntryStatusChange change, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Adds a manual point adjustment. Owner/admin.</summary>
    Task<AdjustmentRecord> AddAdjustmentAsync(AdjustmentRecord adjustment, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Soft-deletes an adjustment. Owner/admin.</summary>
    Task UndoAdjustmentAsync(Guid adjustmentId, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Returns standings for a visible board, ranked.</summary>
    Task<IReadOnlyList<StandingRecord>> GetStandingsAsync(Guid boardId, LeaderboardActor actor, CancellationToken ct = default);

    /// <summary>Recomputes every member standing on a board. Owner/admin.</summary>
    Task RecalculateStandingsAsync(Guid boardId, LeaderboardActor actor, CancellationToken ct = default);
}

/// <summary>Status change payload for <see cref="ILeaderboardStore.SetEntryStatusAsync" />.</summary>
public sealed class EntryStatusChange
{
    /// <summary>Target status.</summary>
    public Enums.EntryStatus Status { get; set; }

    /// <summary>Required when rejecting.</summary>
    public string? RejectionReason { get; set; }
}
