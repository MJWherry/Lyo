using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.FantasyFootball.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.FantasyFootball.Postgres;

/// <summary>Postgres-backed <see cref="IFantasyFootballStore" />.</summary>
public sealed class PostgresFantasyFootballStore(IDbContextFactory<FantasyFootballDbContext> contexts) : IFantasyFootballStore
{
    /// <inheritdoc />
    public async Task<FantasyLeagueRecord> SaveLeagueAsync(FantasyLeagueRecord league, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(league);
        ArgumentHelpers.ThrowIfNegativeOrZero(league.EspnLeagueId);
        ArgumentHelpers.ThrowIfNegativeOrZero(league.SeasonId);
        await using var db = await contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await db.Leagues
            .Include(row => row.Members)
            .Include(row => row.Teams)
            .ThenInclude(team => team.Roster)
            .FirstOrDefaultAsync(row => row.EspnLeagueId == league.EspnLeagueId && row.SeasonId == league.SeasonId, ct)
            .ConfigureAwait(false);
        if (entity == null) {
            entity = new LeagueEntity { Id = league.Id == Guid.Empty ? LyoGuid.CreateV7() : league.Id, EspnLeagueId = league.EspnLeagueId, SeasonId = league.SeasonId };
            db.Leagues.Add(entity);
        }

        entity.Name = league.Name;
        entity.ScoringPeriodId = league.ScoringPeriodId;
        UpsertMembers(db, entity, league.Members);
        UpsertTeams(db, entity, league.Teams, league.ScoringPeriodId);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return (await GetLeagueAsync(league.EspnLeagueId, league.SeasonId, ct).ConfigureAwait(false))!;
    }

    /// <inheritdoc />
    public async Task<FantasyLeagueRecord?> GetLeagueAsync(int espnLeagueId, int seasonId, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await db.Leagues.AsNoTracking()
            .Include(row => row.Members)
            .Include(row => row.Teams)
            .ThenInclude(team => team.Roster)
            .FirstOrDefaultAsync(row => row.EspnLeagueId == espnLeagueId && row.SeasonId == seasonId, ct)
            .ConfigureAwait(false);
        return entity == null ? null : ToLeague(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FantasyConversationRecord>> ListConversationsAsync(int espnLeagueId, int seasonId, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        var league = await FindLeagueIdAsync(db, espnLeagueId, seasonId, ct).ConfigureAwait(false);
        if (league == null)
            return [];

        var rows = await db.Conversations.AsNoTracking()
            .Include(row => row.Members)
            .ThenInclude(link => link.Member)
            .Where(row => row.LeagueId == league.Value)
            .OrderBy(row => row.Type)
            .ThenBy(row => row.EspnTopicId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows.Select(ToConversation).ToArray();
    }

    /// <inheritdoc />
    public async Task SaveConversationsAsync(int espnLeagueId, int seasonId, IReadOnlyList<FantasyConversationRecord> conversations, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(conversations);
        await using var db = await contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        var league = await db.Leagues.Include(row => row.Members).FirstOrDefaultAsync(row => row.EspnLeagueId == espnLeagueId && row.SeasonId == seasonId, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(league == null, "Save the league before saving conversations.");
        var members = league!.Members.ToDictionary(member => member.EspnMemberId, StringComparer.OrdinalIgnoreCase);
        foreach (var conversation in conversations) {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(conversation.EspnTopicId);
            var entity = await db.Conversations.Include(row => row.Members)
                .FirstOrDefaultAsync(row => row.LeagueId == league.Id && row.EspnTopicId == conversation.EspnTopicId, ct)
                .ConfigureAwait(false);
            if (entity == null) {
                entity = new ConversationEntity { Id = conversation.Id == Guid.Empty ? LyoGuid.CreateV7() : conversation.Id, LeagueId = league.Id, EspnTopicId = conversation.EspnTopicId };
                db.Conversations.Add(entity);
            }

            entity.Type = conversation.Type;
            entity.UnreadCount = conversation.UnreadCount;
            entity.TotalMessageCount = conversation.TotalMessageCount;
            entity.Subject = conversation.Subject;
            db.ConversationMembers.RemoveRange(entity.Members);
            entity.Members.Clear();
            foreach (var member in conversation.Members) {
                if (string.IsNullOrWhiteSpace(member.EspnMemberId) || !members.TryGetValue(member.EspnMemberId, out var stored))
                    continue;

                entity.Members.Add(new ConversationMemberEntity { ConversationId = entity.Id, MemberId = stored.Id });
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FantasyMessageRecord>> ListMessagesAsync(int espnLeagueId, int seasonId, string espnTopicId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(espnTopicId);
        await using var db = await contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        var conversation = await FindConversationAsync(db, espnLeagueId, seasonId, espnTopicId, tracking: false, ct).ConfigureAwait(false);
        if (conversation == null)
            return [];

        var rows = await db.Messages.AsNoTracking()
            .Include(row => row.Players)
            .Where(row => row.ConversationId == conversation.Id)
            .OrderBy(row => row.SentAt)
            .ThenBy(row => row.EspnMessageId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows.Select(ToMessage).ToArray();
    }

    /// <inheritdoc />
    public async Task SaveMessagesAsync(int espnLeagueId, int seasonId, string espnTopicId, IReadOnlyList<FantasyMessageRecord> messages, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(messages);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(espnTopicId);
        await using var db = await contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        var league = await db.Leagues.FirstOrDefaultAsync(row => row.EspnLeagueId == espnLeagueId && row.SeasonId == seasonId, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(league == null, "Save the league before saving messages.");
        var conversation = await db.Conversations.FirstOrDefaultAsync(row => row.LeagueId == league!.Id && row.EspnTopicId == espnTopicId, ct).ConfigureAwait(false);
        if (conversation == null) {
            conversation = new ConversationEntity { Id = LyoGuid.CreateV7(), LeagueId = league!.Id, EspnTopicId = espnTopicId };
            db.Conversations.Add(conversation);
        }

        foreach (var message in messages) {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(message.EspnMessageId);
            var entity = await db.Messages.Include(row => row.Players)
                .FirstOrDefaultAsync(row => row.ConversationId == conversation.Id && row.EspnMessageId == message.EspnMessageId, ct)
                .ConfigureAwait(false);
            if (entity == null) {
                entity = new MessageEntity { Id = message.Id == Guid.Empty ? LyoGuid.CreateV7() : message.Id, ConversationId = conversation.Id, EspnMessageId = message.EspnMessageId };
                db.Messages.Add(entity);
            }

            entity.AuthorEspnId = message.AuthorEspnId;
            entity.AuthorName = message.AuthorName;
            entity.Kind = string.IsNullOrWhiteSpace(message.Kind) ? "Text" : message.Kind;
            entity.Content = message.Content;
            entity.SentAt = message.SentAt;
            entity.EspnTransactionId = message.EspnTransactionId;
            db.MessagePlayers.RemoveRange(entity.Players);
            entity.Players.Clear();
            foreach (var player in message.Players)
                entity.Players.Add(new MessagePlayerEntity {
                    Id = LyoGuid.CreateV7(),
                    MessageId = entity.Id,
                    Side = player.Side,
                    EspnTeamId = player.EspnTeamId,
                    PlayerId = player.PlayerId,
                    PlayerName = player.PlayerName,
                    Direction = player.Direction
                });
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FantasyTradeRecord>> ListTradesAsync(int espnLeagueId, int seasonId, bool? pending = null, CancellationToken ct = default)
    {
        await using var db = await contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        var league = await FindLeagueIdAsync(db, espnLeagueId, seasonId, ct).ConfigureAwait(false);
        if (league == null)
            return [];

        var query = db.Trades.AsNoTracking().Include(row => row.Items).Where(row => row.LeagueId == league.Value);
        if (pending != null)
            query = query.Where(row => row.IsPending == pending.Value);

        var rows = await query.OrderBy(row => row.EspnTransactionId).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToTrade).ToArray();
    }

    /// <inheritdoc />
    public async Task SaveTradesAsync(int espnLeagueId, int seasonId, IReadOnlyList<FantasyTradeRecord> trades, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(trades);
        await using var db = await contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        var league = await db.Leagues.FirstOrDefaultAsync(row => row.EspnLeagueId == espnLeagueId && row.SeasonId == seasonId, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(league == null, "Save the league before saving trades.");
        foreach (var trade in trades) {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(trade.EspnTransactionId);
            var entity = await db.Trades.Include(row => row.Items)
                .FirstOrDefaultAsync(row => row.LeagueId == league!.Id && row.EspnTransactionId == trade.EspnTransactionId, ct)
                .ConfigureAwait(false);
            if (entity == null) {
                entity = new TradeEntity { Id = trade.Id == Guid.Empty ? LyoGuid.CreateV7() : trade.Id, LeagueId = league!.Id, EspnTransactionId = trade.EspnTransactionId };
                db.Trades.Add(entity);
            }

            entity.Type = trade.Type;
            entity.Status = trade.Status;
            entity.ScoringPeriodId = trade.ScoringPeriodId;
            entity.IsPending = trade.IsPending;
            entity.Comment = trade.Comment;
            db.TradeItems.RemoveRange(entity.Items);
            entity.Items.Clear();
            foreach (var item in trade.Items)
                entity.Items.Add(new TradeItemEntity {
                    Id = LyoGuid.CreateV7(),
                    TradeId = entity.Id,
                    FromEspnTeamId = item.FromEspnTeamId,
                    ToEspnTeamId = item.ToEspnTeamId,
                    PlayerId = item.PlayerId,
                    PlayerName = item.PlayerName
                });
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static void UpsertMembers(FantasyFootballDbContext db, LeagueEntity league, IReadOnlyList<FantasyMemberRecord> members)
    {
        foreach (var member in members) {
            if (string.IsNullOrWhiteSpace(member.EspnMemberId))
                continue;

            var entity = league.Members.FirstOrDefault(row => string.Equals(row.EspnMemberId, member.EspnMemberId, StringComparison.OrdinalIgnoreCase));
            if (entity == null) {
                entity = new MemberEntity { Id = member.Id == Guid.Empty ? LyoGuid.CreateV7() : member.Id, LeagueId = league.Id, EspnMemberId = member.EspnMemberId };
                league.Members.Add(entity);
                db.Members.Add(entity);
            }

            entity.DisplayName = member.DisplayName;
            entity.FirstName = member.FirstName;
            entity.LastName = member.LastName;
        }
    }

    private static void UpsertTeams(FantasyFootballDbContext db, LeagueEntity league, IReadOnlyList<FantasyTeamRecord> teams, int scoringPeriodId)
    {
        foreach (var team in teams) {
            var entity = league.Teams.FirstOrDefault(row => row.EspnTeamId == team.EspnTeamId);
            if (entity == null) {
                entity = new TeamEntity { Id = team.Id == Guid.Empty ? LyoGuid.CreateV7() : team.Id, LeagueId = league.Id, EspnTeamId = team.EspnTeamId };
                league.Teams.Add(entity);
                db.Teams.Add(entity);
            }

            entity.Name = team.Name;
            entity.Abbrev = team.Abbrev;
            entity.Location = team.Location;
            entity.Nickname = team.Nickname;
            entity.PrimaryOwnerId = team.PrimaryOwnerId;
            ReplaceRoster(db, entity, team.Roster, scoringPeriodId);
        }
    }

    private static void ReplaceRoster(FantasyFootballDbContext db, TeamEntity team, IReadOnlyList<FantasyRosterEntryRecord> roster, int scoringPeriodId)
    {
        var wanted = new List<FantasyRosterEntryRecord>();
        foreach (var entry in roster) {
            var entryPeriod = entry.ScoringPeriodId > 0 ? entry.ScoringPeriodId : scoringPeriodId;
            if (entryPeriod == scoringPeriodId)
                wanted.Add(entry);
        }

        var wantedIds = new HashSet<int>(wanted.Select(entry => entry.PlayerId));
        foreach (var row in team.Roster.Where(row => row.ScoringPeriodId == scoringPeriodId && !wantedIds.Contains(row.PlayerId)).ToArray())
            db.RosterEntries.Remove(row);

        foreach (var entry in wanted) {
            var existing = team.Roster.FirstOrDefault(row => row.ScoringPeriodId == scoringPeriodId && row.PlayerId == entry.PlayerId && db.Entry(row).State != EntityState.Deleted);
            if (existing == null) {
                team.Roster.Add(new RosterEntryEntity {
                    Id = LyoGuid.CreateV7(),
                    TeamId = team.Id,
                    ScoringPeriodId = scoringPeriodId,
                    PlayerId = entry.PlayerId,
                    PlayerName = entry.PlayerName
                });
            }
            else
                existing.PlayerName = entry.PlayerName;
        }
    }

    private static async Task<Guid?> FindLeagueIdAsync(FantasyFootballDbContext db, int espnLeagueId, int seasonId, CancellationToken ct)
        => await db.Leagues.AsNoTracking()
            .Where(row => row.EspnLeagueId == espnLeagueId && row.SeasonId == seasonId)
            .Select(row => (Guid?)row.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    private static async Task<ConversationEntity?> FindConversationAsync(
        FantasyFootballDbContext db, int espnLeagueId, int seasonId, string espnTopicId, bool tracking, CancellationToken ct)
    {
        var query = tracking ? db.Conversations : db.Conversations.AsNoTracking();
        return await query.FirstOrDefaultAsync(
                row => row.EspnTopicId == espnTopicId && row.League.EspnLeagueId == espnLeagueId && row.League.SeasonId == seasonId, ct)
            .ConfigureAwait(false);
    }

    private static FantasyLeagueRecord ToLeague(LeagueEntity entity)
        => new() {
            Id = entity.Id,
            EspnLeagueId = entity.EspnLeagueId,
            SeasonId = entity.SeasonId,
            Name = entity.Name,
            ScoringPeriodId = entity.ScoringPeriodId,
            Members = entity.Members.OrderBy(member => member.DisplayName).Select(ToMember).ToArray(),
            Teams = entity.Teams.OrderBy(team => team.EspnTeamId).Select(team => ToTeam(team, entity.ScoringPeriodId)).ToArray()
        };

    private static FantasyMemberRecord ToMember(MemberEntity entity)
        => new() {
            Id = entity.Id,
            EspnMemberId = entity.EspnMemberId,
            DisplayName = entity.DisplayName,
            FirstName = entity.FirstName,
            LastName = entity.LastName
        };

    private static FantasyTeamRecord ToTeam(TeamEntity entity, int scoringPeriodId)
        => new() {
            Id = entity.Id,
            EspnTeamId = entity.EspnTeamId,
            Name = entity.Name,
            Abbrev = entity.Abbrev,
            Location = entity.Location,
            Nickname = entity.Nickname,
            PrimaryOwnerId = entity.PrimaryOwnerId,
            Roster = entity.Roster.Where(row => row.ScoringPeriodId == scoringPeriodId).OrderBy(row => row.PlayerId).Select(row => new FantasyRosterEntryRecord {
                ScoringPeriodId = row.ScoringPeriodId,
                PlayerId = row.PlayerId,
                PlayerName = row.PlayerName
            }).ToArray()
        };

    private static FantasyConversationRecord ToConversation(ConversationEntity entity)
        => new() {
            Id = entity.Id,
            EspnTopicId = entity.EspnTopicId,
            Type = entity.Type,
            UnreadCount = entity.UnreadCount,
            TotalMessageCount = entity.TotalMessageCount,
            Subject = entity.Subject,
            Members = entity.Members.Select(link => ToMember(link.Member)).OrderBy(member => member.DisplayName).ToArray()
        };

    private static FantasyMessageRecord ToMessage(MessageEntity entity)
        => new() {
            Id = entity.Id,
            EspnMessageId = entity.EspnMessageId,
            AuthorEspnId = entity.AuthorEspnId,
            AuthorName = entity.AuthorName,
            Kind = entity.Kind,
            Content = entity.Content,
            SentAt = entity.SentAt,
            EspnTransactionId = entity.EspnTransactionId,
            Players = entity.Players.OrderBy(player => player.Side).ThenBy(player => player.Direction).ThenBy(player => player.PlayerId).Select(player => new FantasyMessagePlayerRecord {
                Side = player.Side,
                EspnTeamId = player.EspnTeamId,
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
                Direction = player.Direction
            }).ToArray()
        };

    private static FantasyTradeRecord ToTrade(TradeEntity entity)
        => new() {
            Id = entity.Id,
            EspnTransactionId = entity.EspnTransactionId,
            Type = entity.Type,
            Status = entity.Status,
            ScoringPeriodId = entity.ScoringPeriodId,
            IsPending = entity.IsPending,
            Comment = entity.Comment,
            Items = entity.Items.OrderBy(item => item.PlayerId).Select(item => new FantasyTradeItemRecord {
                FromEspnTeamId = item.FromEspnTeamId,
                ToEspnTeamId = item.ToEspnTeamId,
                PlayerId = item.PlayerId,
                PlayerName = item.PlayerName
            }).ToArray()
        };
}
