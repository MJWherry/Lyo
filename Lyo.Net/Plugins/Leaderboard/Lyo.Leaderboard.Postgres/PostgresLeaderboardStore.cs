using Lyo.Algorithms;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Health;
using Lyo.Leaderboard.Enums;
using Lyo.Leaderboard.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Leaderboard.Postgres;

/// <summary>PostgreSQL implementation of <see cref="ILeaderboardStore" />. Board roles are enforced here.</summary>
public sealed class PostgresLeaderboardStore : ILeaderboardStore, IHealth
{
    private readonly LeaderboardCatalog _catalog;
    private readonly IDbContextFactory<LeaderboardDbContext> _factory;

    /// <summary>Creates the store.</summary>
    public PostgresLeaderboardStore(IDbContextFactory<LeaderboardDbContext> factory, IAlgorithmCatalog catalog, IAlgorithmRegistry registry)
    {
        ArgumentHelpers.ThrowIfNull(factory);
        ArgumentHelpers.ThrowIfNull(catalog);
        ArgumentHelpers.ThrowIfNull(registry);
        _factory = factory;
        _catalog = new(catalog, registry);
    }

    /// <inheritdoc />
    public string HealthCheckName => "leaderboard-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => PostgresHealth.CheckAsync(_factory, PostgresLeaderboardOptions.Schema, ct);

    /// <inheritdoc />
    public async Task<BoardRecord> SaveBoardAsync(BoardRecord board, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(board);
        ArgumentHelpers.ThrowIfNull(actor);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(board.Name, nameof(board.Name));
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        if (board.Id == Guid.Empty) {
            var entity = new BoardEntity { Id = LyoGuid.CreateV7(), CreatedAt = now };
            if (string.IsNullOrWhiteSpace(board.Slug))
                board.Slug = BoardSlug.FromName(board.Name);

            board.OwnerUserId = actor.UserId;
            board.TenantId = board.TenantId == Guid.Empty ? actor.UserId : board.TenantId;
            LeaderboardMapper.CopyBoard(board, entity);
            entity.OwnerUserId = actor.UserId;
            db.Boards.Add(entity);
            db.Members.Add(
                new() {
                    Id = LyoGuid.CreateV7(),
                    BoardId = entity.Id,
                    UserId = actor.UserId,
                    DisplayName = actor.DisplayName ?? actor.Email ?? "Owner",
                    Role = LeaderboardMapper.EnumText(MemberRole.Owner),
                    Status = LeaderboardMapper.EnumText(MemberStatus.Active),
                    JoinedAt = now,
                    CreatedAt = now
                });

            entity.AlgorithmKey = LeaderboardCatalog.KeyFor(entity.Slug);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await _catalog.SaveFormulaAsync(entity.AlgorithmKey, board.ScoreFormula, ct).ConfigureAwait(false);
            return await PresentBoardAsync(entity, ct).ConfigureAwait(false);
        }

        var existing = await RequireBoardAsync(db, board.Id, ct).ConfigureAwait(false);
        await RequireRoleAsync(db, existing.Id, actor, MemberRole.Owner, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(board.Slug))
            board.Slug = existing.Slug;

        board.OwnerUserId = existing.OwnerUserId;
        LeaderboardMapper.CopyBoard(board, existing);
        if (string.IsNullOrWhiteSpace(existing.AlgorithmKey))
            existing.AlgorithmKey = LeaderboardCatalog.KeyFor(existing.Slug);

        existing.UpdatedAt = now;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await _catalog.SaveFormulaAsync(existing.AlgorithmKey, board.ScoreFormula, ct).ConfigureAwait(false);
        return await PresentBoardAsync(existing, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BoardRecord?> GetBoardByIdAsync(Guid id, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await db.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null, ct).ConfigureAwait(false);
        if (entity == null || !await CanSeeAsync(db, entity, actor, ct).ConfigureAwait(false))
            return null;

        return await PresentBoardAsync(entity, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BoardRecord?> GetBoardBySlugAsync(string slug, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var trimmed = slug.Trim();
        var entity = await db.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Slug == trimmed && b.DeletedAt == null, ct).ConfigureAwait(false);
        if (entity == null || !await CanSeeAsync(db, entity, actor, ct).ConfigureAwait(false))
            return null;

        return await PresentBoardAsync(entity, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ArchiveBoardAsync(Guid boardId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var board = await RequireBoardAsync(db, boardId, ct).ConfigureAwait(false);
        await RequireRoleAsync(db, board.Id, actor, MemberRole.Owner, ct).ConfigureAwait(false);
        board.ArchivedAt = DateTime.UtcNow;
        board.UpdatedAt = board.ArchivedAt;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ScoringPropertyRecord> SavePropertyAsync(ScoringPropertyRecord property, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(property);
        ArgumentHelpers.ThrowIfNull(actor);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(property.Key, nameof(property.Key));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(property.Label, nameof(property.Label));
        if (!BoardSlug.IsPropertyKey(property.Key))
            throw new BadRequestException($"Property key '{property.Key}' must match [a-z][a-z0-9_]*.");

        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var board = await RequireBoardAsync(db, property.BoardId, ct).ConfigureAwait(false);
        await RequireRoleAsync(db, board.Id, actor, MemberRole.Owner, ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        if (property.Id != Guid.Empty) {
            var existing = await db.Properties.FirstOrDefaultAsync(p => p.Id == property.Id && p.BoardId == board.Id, ct).ConfigureAwait(false);
            if (existing == null)
                throw NotFoundException.ForResource("ScoringProperty", property.Id);

            if ((existing.Key != property.Key || existing.ValueKind != LeaderboardMapper.EnumText(property.ValueKind)) &&
                await HasApprovedEntriesAsync(db, board.Id, ct).ConfigureAwait(false))
                throw new BadRequestException("Cannot change a property key or value kind after approved entries exist.");

            LeaderboardMapper.CopyProperty(property, existing);
            existing.UpdatedAt = now;
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return LeaderboardMapper.ToProperty(existing);
        }

        var entity = new ScoringPropertyEntity { Id = LyoGuid.CreateV7(), CreatedAt = now };
        LeaderboardMapper.CopyProperty(property, entity);
        db.Properties.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return LeaderboardMapper.ToProperty(entity);
    }

    /// <inheritdoc />
    public async Task DeletePropertyAsync(Guid propertyId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var property = await db.Properties.FirstOrDefaultAsync(p => p.Id == propertyId, ct).ConfigureAwait(false);
        if (property == null)
            throw NotFoundException.ForResource("ScoringProperty", propertyId);

        await RequireRoleAsync(db, property.BoardId, actor, MemberRole.Owner, ct).ConfigureAwait(false);
        if (await HasApprovedEntriesAsync(db, property.BoardId, ct).ConfigureAwait(false))
            throw new BadRequestException("Cannot delete a scoring property after approved entries exist.");

        db.Properties.Remove(property);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScoringPropertyRecord>> ListPropertiesAsync(Guid boardId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var board = await RequireVisibleBoardAsync(db, boardId, actor, ct).ConfigureAwait(false);
        var rows = await db.Properties.AsNoTracking().Where(p => p.BoardId == board.Id).OrderBy(p => p.SortOrder).ThenBy(p => p.Key).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(LeaderboardMapper.ToProperty).ToArray();
    }

    /// <inheritdoc />
    public async Task<MemberRecord> SaveMemberAsync(MemberRecord member, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(member);
        ArgumentHelpers.ThrowIfNull(actor);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(member.DisplayName, nameof(member.DisplayName));
        if (member.Role == MemberRole.Owner)
            throw new BadRequestException("Owner cannot be assigned through SaveMember.");

        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await RequireRoleAsync(db, member.BoardId, actor, MemberRole.Admin, ct).ConfigureAwait(false);
        var existing = await db.Members.FirstOrDefaultAsync(m => m.Id == member.Id, ct).ConfigureAwait(false);
        if (existing == null)
            throw NotFoundException.ForResource("Member", member.Id);

        if (LeaderboardMapper.ParseEnum(existing.Role, MemberRole.Contributor) == MemberRole.Owner)
            throw new ForbiddenException("The board owner cannot be modified this way.");

        existing.DisplayName = member.DisplayName;
        existing.Role = LeaderboardMapper.EnumText(member.Role);
        existing.Status = LeaderboardMapper.EnumText(member.Status);
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return LeaderboardMapper.ToMember(existing);
    }

    /// <inheritdoc />
    public async Task RemoveMemberAsync(Guid memberId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == memberId, ct).ConfigureAwait(false);
        if (member == null)
            throw NotFoundException.ForResource("Member", memberId);

        await RequireRoleAsync(db, member.BoardId, actor, MemberRole.Admin, ct).ConfigureAwait(false);
        if (LeaderboardMapper.ParseEnum(member.Role, MemberRole.Contributor) == MemberRole.Owner)
            throw new ForbiddenException("The board owner cannot be removed.");

        member.Status = LeaderboardMapper.EnumText(MemberStatus.Removed);
        member.RemovedAt = DateTime.UtcNow;
        member.UpdatedAt = member.RemovedAt;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecalculateBoardAsync(db, member.BoardId, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MemberRecord>> ListMembersAsync(Guid boardId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var board = await RequireVisibleBoardAsync(db, boardId, actor, ct).ConfigureAwait(false);
        var rows = await db.Members.AsNoTracking().Where(m => m.BoardId == board.Id).OrderBy(m => m.DisplayName).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(LeaderboardMapper.ToMember).ToArray();
    }

    /// <inheritdoc />
    public async Task<InviteCreateResult> CreateInviteAsync(Guid boardId, InviteCreateRequest request, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(actor);
        if (request.Role is MemberRole.Owner)
            throw new BadRequestException("Invites cannot grant the owner role.");

        if (request.ShareKind == InviteShareKind.Email && string.IsNullOrWhiteSpace(request.Email))
            throw new BadRequestException("Email invites require an email address.");

        if (request.ShareKind == InviteShareKind.User && request.InvitedUserId == null)
            throw new BadRequestException("User invites require an invited user id.");

        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var board = await RequireBoardAsync(db, boardId, ct).ConfigureAwait(false);
        await RequireRoleAsync(db, board.Id, actor, MemberRole.Admin, ct).ConfigureAwait(false);
        var token = InviteToken.Create();
        var now = DateTime.UtcNow;
        var unlimited = request.ShareKind is InviteShareKind.Link or InviteShareKind.Qr;
        var invite = new InviteEntity {
            Id = LyoGuid.CreateV7(),
            BoardId = board.Id,
            TokenHash = InviteToken.Hash(token),
            ShareKind = LeaderboardMapper.EnumText(request.ShareKind),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            InvitedUserId = request.InvitedUserId,
            Role = LeaderboardMapper.EnumText(request.Role),
            MaxUses = unlimited ? 0 : 1,
            CreatedByUserId = actor.UserId,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = now
        };

        db.Invites.Add(invite);
        MemberEntity? member = null;
        if (request.ShareKind is InviteShareKind.Email or InviteShareKind.User) {
            member = new() {
                Id = LyoGuid.CreateV7(),
                BoardId = board.Id,
                UserId = request.InvitedUserId,
                DisplayName = request.DisplayName ?? request.Email ?? "Invited member",
                Role = invite.Role,
                Status = LeaderboardMapper.EnumText(MemberStatus.Invited),
                InvitedByUserId = actor.UserId,
                CreatedAt = now
            };

            db.Members.Add(member);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new() {
            Invite = LeaderboardMapper.ToInvite(invite),
            Token = token,
            JoinPath = LeaderboardJoinUrl.BuildPath(board.Slug, token),
            Member = member == null ? null : LeaderboardMapper.ToMember(member)
        };
    }

    /// <inheritdoc />
    public async Task AttachShortUrlAsync(Guid inviteId, string shortUrlId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(shortUrlId);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var invite = await db.Invites.FirstOrDefaultAsync(i => i.Id == inviteId, ct).ConfigureAwait(false);
        if (invite == null)
            throw NotFoundException.ForResource("Invite", inviteId);

        await RequireRoleAsync(db, invite.BoardId, actor, MemberRole.Admin, ct).ConfigureAwait(false);
        invite.ShortUrlId = shortUrlId.Trim();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InviteRecord?> GetInviteByIdAsync(Guid inviteId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var invite = await db.Invites.AsNoTracking().FirstOrDefaultAsync(i => i.Id == inviteId, ct).ConfigureAwait(false);
        if (invite == null)
            return null;

        await RequireRoleAsync(db, invite.BoardId, actor, MemberRole.Admin, ct).ConfigureAwait(false);
        return LeaderboardMapper.ToInvite(invite);
    }

    /// <inheritdoc />
    public async Task<MemberRecord> RedeemInviteAsync(string token, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(token);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var hash = InviteToken.Hash(token);
        var invite = await db.Invites.FirstOrDefaultAsync(i => i.TokenHash == hash, ct).ConfigureAwait(false);
        if (invite == null)
            throw NotFoundException.ForResource("Invite", token);

        if (invite.RevokedAt != null)
            throw new BadRequestException("This invite has been revoked.");

        if (invite.ExpiresAt is { } expires && expires <= DateTime.UtcNow)
            throw new BadRequestException("This invite has expired.");

        if (invite.MaxUses > 0 && invite.UseCount >= invite.MaxUses)
            throw new BadRequestException("This invite has no remaining uses.");

        if (!string.IsNullOrWhiteSpace(invite.Email) && !string.Equals(invite.Email, actor.Email, StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("This invite is locked to a different email address.");

        var now = DateTime.UtcNow;
        var existing = await db.Members.FirstOrDefaultAsync(m => m.BoardId == invite.BoardId && m.UserId == actor.UserId, ct).ConfigureAwait(false);
        if (existing == null && invite.InvitedUserId == actor.UserId)
            existing = await db.Members.FirstOrDefaultAsync(m => m.BoardId == invite.BoardId && m.UserId == invite.InvitedUserId, ct).ConfigureAwait(false);

        if (existing == null && !string.IsNullOrWhiteSpace(invite.Email)) {
            existing = await db.Members.FirstOrDefaultAsync(m => m.BoardId == invite.BoardId && m.UserId == null && m.DisplayName == invite.Email, ct).ConfigureAwait(false);
        }

        if (existing == null) {
            existing = new() { Id = LyoGuid.CreateV7(), BoardId = invite.BoardId, CreatedAt = now };
            db.Members.Add(existing);
        }

        if (LeaderboardMapper.ParseEnum(existing.Status, MemberStatus.Invited) == MemberStatus.Banned)
            throw new ForbiddenException("You are banned from this board.");

        existing.UserId = actor.UserId;
        existing.DisplayName = actor.DisplayName ?? actor.Email ?? existing.DisplayName;
        existing.Role = invite.Role;
        existing.Status = LeaderboardMapper.EnumText(MemberStatus.Active);
        existing.JoinedAt = now;
        existing.UpdatedAt = now;
        invite.UseCount += 1;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return LeaderboardMapper.ToMember(existing);
    }

    /// <inheritdoc />
    public async Task RevokeInviteAsync(Guid inviteId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var invite = await db.Invites.FirstOrDefaultAsync(i => i.Id == inviteId, ct).ConfigureAwait(false);
        if (invite == null)
            throw NotFoundException.ForResource("Invite", inviteId);

        await RequireRoleAsync(db, invite.BoardId, actor, MemberRole.Admin, ct).ConfigureAwait(false);
        invite.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<EntryRecord> SaveEntryAsync(EntryRecord entry, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(entry);
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var board = await RequireBoardAsync(db, entry.BoardId, ct).ConfigureAwait(false);
        if (board.ArchivedAt != null)
            throw new BadRequestException("This board is archived.");

        if (board.StartsAt is { } start && entry.OccurredAt < start)
            throw new BadRequestException("This entry is before the board start time.");

        if (board.EndsAt is { } end && entry.OccurredAt > end)
            throw new BadRequestException("This entry is after the board end time.");

        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == entry.MemberId && m.BoardId == board.Id, ct).ConfigureAwait(false);
        if (member == null)
            throw NotFoundException.ForResource("Member", entry.MemberId);

        var actorMember = await FindMemberAsync(db, board.Id, actor.UserId, ct).ConfigureAwait(false);
        var actorRole = actorMember == null ? (MemberRole?)null : LeaderboardMapper.ParseEnum(actorMember.Role, MemberRole.Viewer);
        var isAdmin = actorRole is MemberRole.Admin or MemberRole.Owner;
        if (!isAdmin) {
            if (!board.MembersCanSubmit)
                throw new ForbiddenException("Members cannot submit entries on this board.");

            if (actorRole != MemberRole.Contributor || member.UserId != actor.UserId)
                throw new ForbiddenException("You cannot submit an entry for this member.");
        }

        if (!board.AllowMultipleEntries && entry.Id == Guid.Empty) {
            var exists = await db.Entries.AnyAsync(e => e.BoardId == board.Id && e.MemberId == member.Id && e.DeletedAt == null, ct).ConfigureAwait(false);
            if (exists)
                throw new BadRequestException("This board allows only one entry per member.");
        }

        var properties = await db.Properties.Where(p => p.BoardId == board.Id).OrderBy(p => p.SortOrder).ToListAsync(ct).ConfigureAwait(false);
        foreach (var value in entry.Values) {
            var property = properties.FirstOrDefault(p => p.Id == value.PropertyId);
            if (property != null)
                value.PropertyKey = property.Key;
        }

        var scored = await _catalog.ScoreAsync(board.AlgorithmKey, properties.Select(LeaderboardMapper.ToProperty).ToArray(), entry.Values, ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var status = isAdmin || !board.RequireVerification ? EntryStatus.Approved : EntryStatus.Pending;
        EntryEntity entity;
        if (entry.Id != Guid.Empty) {
            entity = await db.Entries.Include(e => e.Values).FirstOrDefaultAsync(e => e.Id == entry.Id && e.DeletedAt == null, ct).ConfigureAwait(false) ??
                throw NotFoundException.ForResource("Entry", entry.Id);

            var current = LeaderboardMapper.ParseEnum(entity.Status, EntryStatus.Draft);
            if (current == EntryStatus.Approved && !isAdmin)
                throw new ForbiddenException("Approved entries can only be edited by an admin.");

            if (!isAdmin && !(board.MembersCanEditOwn && current is EntryStatus.Draft or EntryStatus.Pending && entity.SubmittedByUserId == actor.UserId))
                throw new ForbiddenException("You cannot edit this entry.");

            db.EntryValues.RemoveRange(entity.Values);
        }
        else {
            entity = new() {
                Id = LyoGuid.CreateV7(),
                CreatedAt = now,
                BoardId = board.Id,
                MemberId = member.Id
            };

            db.Entries.Add(entity);
        }

        entity.SubmittedByUserId = actor.UserId;
        entity.Title = entry.Title;
        entity.Notes = entry.Notes;
        entity.OccurredAt = entry.OccurredAt == default ? now : entry.OccurredAt;
        entity.Status = LeaderboardMapper.EnumText(status);
        entity.ComputedScore = scored.Total;
        entity.ScoreBreakdownJson = scored.Breakdown;
        entity.UpdatedAt = now;
        entity.VerifiedByUserId = status == EntryStatus.Approved && isAdmin ? actor.UserId : null;
        entity.VerifiedAt = status == EntryStatus.Approved && isAdmin ? now : null;
        foreach (var value in entry.Values) {
            db.EntryValues.Add(
                new() {
                    Id = LyoGuid.CreateV7(),
                    EntryId = entity.Id,
                    PropertyId = value.PropertyId,
                    ChoiceKey = value.ChoiceKey,
                    NumberValue = value.NumberValue,
                    TextValue = value.TextValue,
                    BoolValue = value.BoolValue,
                    DateTimeValue = value.DateTimeValue
                });
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        if (status == EntryStatus.Approved) {
            await RecalculateBoardAsync(db, board.Id, ct).ConfigureAwait(false);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return await LoadEntryAsync(db, entity.Id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<EntryRecord?> GetEntryByIdAsync(Guid entryId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await db.Entries.AsNoTracking().Include(e => e.Values).FirstOrDefaultAsync(e => e.Id == entryId && e.DeletedAt == null, ct).ConfigureAwait(false);
        if (entity == null)
            return null;

        var board = await db.Boards.AsNoTracking().FirstAsync(b => b.Id == entity.BoardId, ct).ConfigureAwait(false);
        if (!await CanSeeAsync(db, board, actor, ct).ConfigureAwait(false))
            return null;

        var record = LeaderboardMapper.ToEntry(entity);
        var actorMember = await FindMemberAsync(db, entity.BoardId, actor.UserId, ct).ConfigureAwait(false);
        var role = actorMember == null ? (MemberRole?)null : LeaderboardMapper.ParseEnum(actorMember.Role, MemberRole.Viewer);
        if (record.Status != EntryStatus.Approved && role is not (MemberRole.Admin or MemberRole.Owner) && entity.SubmittedByUserId != actor.UserId)
            return null;

        return record;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntryRecord>> ListEntriesAsync(Guid boardId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var board = await RequireVisibleBoardAsync(db, boardId, actor, ct).ConfigureAwait(false);
        var rows = await db.Entries.AsNoTracking()
            .Include(e => e.Values)
            .Where(e => e.BoardId == board.Id && e.DeletedAt == null)
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var actorMember = await FindMemberAsync(db, board.Id, actor.UserId, ct).ConfigureAwait(false);
        var role = actorMember == null ? (MemberRole?)null : LeaderboardMapper.ParseEnum(actorMember.Role, MemberRole.Viewer);
        var isAdmin = role is MemberRole.Admin or MemberRole.Owner;
        var list = new List<EntryRecord>();
        foreach (var entity in rows) {
            var record = LeaderboardMapper.ToEntry(entity);
            if (record.Status != EntryStatus.Approved && !isAdmin && entity.SubmittedByUserId != actor.UserId)
                continue;

            list.Add(record);
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<EntryRecord> SetEntryStatusAsync(Guid entryId, EntryStatusChange change, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(change);
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await db.Entries.FirstOrDefaultAsync(e => e.Id == entryId && e.DeletedAt == null, ct).ConfigureAwait(false) ??
            throw NotFoundException.ForResource("Entry", entryId);

        var actorMember = await FindMemberAsync(db, entity.BoardId, actor.UserId, ct).ConfigureAwait(false);
        var role = actorMember == null ? (MemberRole?)null : LeaderboardMapper.ParseEnum(actorMember.Role, MemberRole.Viewer);
        var isAdmin = role is MemberRole.Admin or MemberRole.Owner;
        if (change.Status is EntryStatus.Approved or EntryStatus.Rejected) {
            if (!isAdmin)
                throw new ForbiddenException("Only an owner or admin can verify entries.");

            if (change.Status == EntryStatus.Rejected && string.IsNullOrWhiteSpace(change.RejectionReason))
                throw new BadRequestException("A rejection reason is required.");

            entity.VerifiedByUserId = actor.UserId;
            entity.VerifiedAt = DateTime.UtcNow;
            entity.RejectionReason = change.Status == EntryStatus.Rejected ? change.RejectionReason : null;
        }
        else if (change.Status == EntryStatus.Withdrawn) {
            if (!isAdmin && entity.SubmittedByUserId != actor.UserId)
                throw new ForbiddenException("You cannot withdraw this entry.");

            var current = LeaderboardMapper.ParseEnum(entity.Status, EntryStatus.Draft);
            if (current == EntryStatus.Approved && !isAdmin)
                throw new ForbiddenException("Approved entries cannot be withdrawn.");
        }
        else
            throw new BadRequestException("Unsupported entry status change.");

        entity.Status = LeaderboardMapper.EnumText(change.Status);
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecalculateBoardAsync(db, entity.BoardId, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return await LoadEntryAsync(db, entity.Id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AdjustmentRecord> AddAdjustmentAsync(AdjustmentRecord adjustment, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(adjustment);
        ArgumentHelpers.ThrowIfNull(actor);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(adjustment.Reason, nameof(adjustment.Reason));
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await RequireRoleAsync(db, adjustment.BoardId, actor, MemberRole.Admin, ct).ConfigureAwait(false);
        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == adjustment.MemberId && m.BoardId == adjustment.BoardId, ct).ConfigureAwait(false);
        if (member == null)
            throw NotFoundException.ForResource("Member", adjustment.MemberId);

        var entity = new AdjustmentEntity {
            Id = LyoGuid.CreateV7(),
            BoardId = adjustment.BoardId,
            MemberId = adjustment.MemberId,
            EntryId = adjustment.EntryId,
            Delta = adjustment.Delta,
            Reason = adjustment.Reason.Trim(),
            CreatedByUserId = actor.UserId,
            CreatedAt = DateTime.UtcNow
        };

        db.Adjustments.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecalculateBoardAsync(db, adjustment.BoardId, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return LeaderboardMapper.ToAdjustment(entity);
    }

    /// <inheritdoc />
    public async Task UndoAdjustmentAsync(Guid adjustmentId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await db.Adjustments.FirstOrDefaultAsync(a => a.Id == adjustmentId && a.DeletedAt == null, ct).ConfigureAwait(false) ??
            throw NotFoundException.ForResource("Adjustment", adjustmentId);

        await RequireRoleAsync(db, entity.BoardId, actor, MemberRole.Admin, ct).ConfigureAwait(false);
        entity.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecalculateBoardAsync(db, entity.BoardId, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StandingRecord>> GetStandingsAsync(Guid boardId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var board = await RequireVisibleBoardAsync(db, boardId, actor, ct).ConfigureAwait(false);
        var rows = await db.Standings.AsNoTracking().Where(s => s.BoardId == board.Id).OrderBy(s => s.Rank).ToListAsync(ct).ConfigureAwait(false);
        var memberIds = rows.Select(r => r.MemberId).ToArray();
        var members = await db.Members.AsNoTracking().Where(m => memberIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, ct).ConfigureAwait(false);
        return rows.Select(r => LeaderboardMapper.ToStanding(r, members.GetValueOrDefault(r.MemberId))).ToArray();
    }

    /// <inheritdoc />
    public async Task RecalculateStandingsAsync(Guid boardId, LeaderboardActor actor, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(actor);
        await using var db = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await RequireRoleAsync(db, boardId, actor, MemberRole.Admin, ct).ConfigureAwait(false);
        await RecalculateBoardAsync(db, boardId, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task RecalculateBoardAsync(LeaderboardDbContext db, Guid boardId, CancellationToken ct)
    {
        var board = await db.Boards.FirstAsync(b => b.Id == boardId, ct).ConfigureAwait(false);
        var aggregation = LeaderboardMapper.ParseEnum(board.AggregationKind, AggregationKind.Sum);
        var sort = LeaderboardMapper.ParseEnum(board.SortDirection, SortDirection.Desc);
        var members = await db.Members.Where(m => m.BoardId == boardId && m.Status == LeaderboardMapper.EnumText(MemberStatus.Active)).ToListAsync(ct).ConfigureAwait(false);
        var approved = LeaderboardMapper.EnumText(EntryStatus.Approved);
        var entries = await db.Entries.AsNoTracking().Where(e => e.BoardId == boardId && e.DeletedAt == null && e.Status == approved).ToListAsync(ct).ConfigureAwait(false);
        var adjustments = await db.Adjustments.AsNoTracking().Where(a => a.BoardId == boardId && a.DeletedAt == null).ToListAsync(ct).ConfigureAwait(false);
        var existing = await db.Standings.Where(s => s.BoardId == boardId).ToListAsync(ct).ConfigureAwait(false);
        db.Standings.RemoveRange(existing);
        var now = DateTime.UtcNow;
        var computed = new List<StandingEntity>();
        foreach (var member in members) {
            var memberEntries = entries.Where(e => e.MemberId == member.Id).Select(e => new ApprovedEntryScore(e.ComputedScore, e.OccurredAt)).ToArray();
            var entryScore = LeaderboardAggregator.Aggregate(aggregation, board.AggregationN, sort, memberEntries);
            var adjustmentScore = adjustments.Where(a => a.MemberId == member.Id).Sum(a => a.Delta);
            computed.Add(
                new() {
                    BoardId = boardId,
                    MemberId = member.Id,
                    EntryScore = entryScore,
                    AdjustmentScore = adjustmentScore,
                    TotalScore = entryScore + adjustmentScore,
                    EntryCount = memberEntries.Length,
                    LastEntryAt = memberEntries.Length == 0 ? null : memberEntries.Max(e => e.OccurredAt),
                    ComputedAt = now
                });
        }

        IEnumerable<StandingEntity> ranked = sort == SortDirection.Asc
            ? computed.OrderBy(s => s.TotalScore).ThenBy(s => s.LastEntryAt ?? DateTime.MaxValue)
            : computed.OrderByDescending(s => s.TotalScore).ThenBy(s => s.LastEntryAt ?? DateTime.MaxValue);

        var rank = 1;
        foreach (var row in ranked) {
            row.Rank = rank++;
            db.Standings.Add(row);
        }
    }

    private async Task<BoardRecord> PresentBoardAsync(BoardEntity entity, CancellationToken ct)
    {
        var record = LeaderboardMapper.ToBoard(entity);
        await _catalog.ApplyFormulaAsync(record, ct).ConfigureAwait(false);
        return record;
    }

    private static async Task<EntryRecord> LoadEntryAsync(LeaderboardDbContext db, Guid id, CancellationToken ct)
    {
        var entity = await db.Entries.AsNoTracking().Include(e => e.Values).FirstAsync(e => e.Id == id, ct).ConfigureAwait(false);
        return LeaderboardMapper.ToEntry(entity);
    }

    private static async Task<bool> HasApprovedEntriesAsync(LeaderboardDbContext db, Guid boardId, CancellationToken ct)
    {
        var approved = LeaderboardMapper.EnumText(EntryStatus.Approved);
        return await db.Entries.AnyAsync(e => e.BoardId == boardId && e.DeletedAt == null && e.Status == approved, ct).ConfigureAwait(false);
    }

    private static async Task<BoardEntity> RequireBoardAsync(LeaderboardDbContext db, Guid boardId, CancellationToken ct)
    {
        var board = await db.Boards.FirstOrDefaultAsync(b => b.Id == boardId && b.DeletedAt == null, ct).ConfigureAwait(false);
        if (board == null)
            throw NotFoundException.ForResource("Board", boardId);

        return board;
    }

    private async Task<BoardEntity> RequireVisibleBoardAsync(LeaderboardDbContext db, Guid boardId, LeaderboardActor actor, CancellationToken ct)
    {
        var board = await RequireBoardAsync(db, boardId, ct).ConfigureAwait(false);
        if (!await CanSeeAsync(db, board, actor, ct).ConfigureAwait(false))
            throw NotFoundException.ForResource("Board", boardId);

        return board;
    }

    private static async Task<bool> CanSeeAsync(LeaderboardDbContext db, BoardEntity board, LeaderboardActor actor, CancellationToken ct)
    {
        var visibility = LeaderboardMapper.ParseEnum(board.Visibility, BoardVisibility.Private);
        if (visibility is BoardVisibility.Public or BoardVisibility.Unlisted)
            return true;

        return await FindMemberAsync(db, board.Id, actor.UserId, ct).ConfigureAwait(false) != null;
    }

    private static Task<MemberEntity?> FindMemberAsync(LeaderboardDbContext db, Guid boardId, Guid userId, CancellationToken ct)
        => db.Members.FirstOrDefaultAsync(
            m => m.BoardId == boardId && m.UserId == userId && m.Status != LeaderboardMapper.EnumText(MemberStatus.Removed) &&
                m.Status != LeaderboardMapper.EnumText(MemberStatus.Banned), ct);

    private static async Task RequireRoleAsync(LeaderboardDbContext db, Guid boardId, LeaderboardActor actor, MemberRole minimum, CancellationToken ct)
    {
        var member = await FindMemberAsync(db, boardId, actor.UserId, ct).ConfigureAwait(false);
        if (member == null)
            throw ForbiddenException.ForResource("Board", boardId, "You are not a member of this board.");

        var role = LeaderboardMapper.ParseEnum(member.Role, MemberRole.Viewer);
        if (role < minimum)
            throw ForbiddenException.ForResource("Board", boardId, $"Requires {minimum}.");
    }
}