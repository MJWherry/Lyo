using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Leaderboard.Enums;
using Lyo.Leaderboard.Postgres;
using Lyo.Leaderboard.Postgres.Database;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Leaderboard.Seed;

/// <summary>EF seed of fishing boards with properties, members, verified entries, an adjustment, and standings. Schema <c>leaderboard</c>.</summary>
public sealed class LeaderboardEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <summary>Score for bass at weight 6 and length 15.</summary>
    public const decimal BassScore = 38m;

    /// <summary>Tenant id written on every seeded board.</summary>
    public static readonly Guid TenantId = new("11111111-1111-7111-8111-111111111111");

    /// <summary>Owner user id written on every seeded board.</summary>
    public static readonly Guid OwnerUserId = new("33333333-3333-7333-8333-333333333333");

    /// <inheritdoc />
    public override string Name => "Leaderboard";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresLeaderboardOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<LeaderboardDbContext>().UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;
        return new LeaderboardDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var now = DateTime.UtcNow;
        var fishChoices = LeaderboardJson.Serialize(
            new List<ScoringChoice> { new() { Key = "bass", Label = "Bass", Value = 10 }, new() { Key = "catfish", Label = "Catfish", Value = 5 } });

        var boardIds = Enumerable.Range(0, options.Count).Select(_ => LyoGuid.CreateV7()).ToArray();
        var fishIds = Enumerable.Range(0, options.Count).Select(_ => LyoGuid.CreateV7()).ToArray();
        var weightIds = Enumerable.Range(0, options.Count).Select(_ => LyoGuid.CreateV7()).ToArray();
        var lengthIds = Enumerable.Range(0, options.Count).Select(_ => LyoGuid.CreateV7()).ToArray();
        var members = Enumerable.Range(0, options.Count).Select(_ => Enumerable.Range(0, 3).Select(_ => LyoGuid.CreateV7()).ToArray()).ToArray();
        var entryBoard = new Dictionary<Guid, int>();
        var boards = graph.Entity(
            options.Count, i => new BoardEntity {
                Id = boardIds[i],
                Slug = $"seed-fishing-{i}",
                Name = $"Bass derby {i}",
                Description = "Saturday weigh-in",
                OwnerUserId = OwnerUserId,
                TenantId = TenantId,
                Visibility = nameof(BoardVisibility.Unlisted),
                JoinPolicy = nameof(JoinPolicy.InviteOnly),
                MembersCanSubmit = true,
                AllowMultipleEntries = true,
                AggregationKind = nameof(AggregationKind.Sum),
                SortDirection = nameof(SortDirection.Desc),
                TieBreak = nameof(TieBreakKind.FirstSubmitted),
                AlgorithmKey = LeaderboardCatalog.KeyFor($"seed-fishing-{i}"),
                CreatedAt = now
            });

        boards.ForEach(_ => 1, (board, _) => Property(fishIds[Index(boardIds, board.Id)], board.Id, "fish_type", "Fish", nameof(ScoringValueKind.Choice), 0, fishChoices, now));
        boards.ForEach(_ => 1, (board, _) => Property(weightIds[Index(boardIds, board.Id)], board.Id, "weight", "Weight", nameof(ScoringValueKind.Number), 1, null, now));
        boards.ForEach(_ => 1, (board, _) => Property(lengthIds[Index(boardIds, board.Id)], board.Id, "length", "Length", nameof(ScoringValueKind.Number), 2, null, now));
        boards.ForEach(
            _ => 3, (board, n) => new MemberEntity {
                Id = members[Index(boardIds, board.Id)][n],
                BoardId = board.Id,
                UserId = n == 0 ? OwnerUserId : LyoGuid.CreateV7(),
                DisplayName = n == 0 ? "Owner" : $"Angler {n}",
                Role = n == 0 ? nameof(MemberRole.Owner) : nameof(MemberRole.Contributor),
                Status = nameof(MemberStatus.Active),
                JoinedAt = now,
                CreatedAt = now
            });

        boards.ForEach(
            _ => 1, (board, _) => new InviteEntity {
                Id = LyoGuid.CreateV7(),
                BoardId = board.Id,
                TokenHash = board.Id.ToString("N"),
                ShareKind = nameof(InviteShareKind.Link),
                Role = nameof(MemberRole.Contributor),
                MaxUses = 1,
                CreatedByUserId = OwnerUserId,
                CreatedAt = now
            });

        var entries = boards.ForEach(
            _ => 2, (board, n) => {
                var index = Index(boardIds, board.Id);
                var entry = new EntryEntity {
                    Id = LyoGuid.CreateV7(),
                    BoardId = board.Id,
                    MemberId = members[index][n],
                    SubmittedByUserId = OwnerUserId,
                    Title = n == 0 ? "Bass" : "Another bass",
                    OccurredAt = now,
                    Status = nameof(EntryStatus.Approved),
                    VerifiedByUserId = OwnerUserId,
                    VerifiedAt = now,
                    ComputedScore = BassScore,
                    CreatedAt = now
                };

                entryBoard[entry.Id] = index;
                return entry;
            });

        entries.ForEach(
            _ => 3, (entry, n) => {
                var index = entryBoard[entry.Id];
                return new EntryValueEntity {
                    Id = LyoGuid.CreateV7(),
                    EntryId = entry.Id,
                    PropertyId = n switch {
                        0 => fishIds[index],
                        1 => weightIds[index],
                        var _ => lengthIds[index]
                    },
                    ChoiceKey = n == 0 ? "bass" : null,
                    NumberValue = n == 0 ? null : n == 1 ? 6m : 15m
                };
            });

        boards.ForEach(
            _ => 1, (board, _) => {
                var index = Index(boardIds, board.Id);
                return new AdjustmentEntity {
                    Id = LyoGuid.CreateV7(),
                    BoardId = board.Id,
                    MemberId = members[index][0],
                    Delta = 1,
                    Reason = "Weigh-in correction",
                    CreatedByUserId = OwnerUserId,
                    CreatedAt = now
                };
            });

        boards.ForEach(
            _ => 3, (board, n) => {
                var index = Index(boardIds, board.Id);
                var entryScore = n == 0 ? BassScore : n == 1 ? BassScore : 0m;
                var adjustment = n == 0 ? 1m : 0m;
                return new StandingEntity {
                    BoardId = board.Id,
                    MemberId = members[index][n],
                    EntryScore = entryScore,
                    AdjustmentScore = adjustment,
                    TotalScore = entryScore + adjustment,
                    EntryCount = n < 2 ? 1 : 0,
                    LastEntryAt = n < 2 ? now : null,
                    Rank = n + 1,
                    ComputedAt = now
                };
            });
    }

    private static int Index(Guid[] ids, Guid id) => Array.IndexOf(ids, id);

    private static ScoringPropertyEntity Property(Guid id, Guid boardId, string key, string label, string kind, int sort, string? choices, DateTime now)
        => new() {
            Id = id,
            BoardId = boardId,
            Key = key,
            Label = label,
            ValueKind = kind,
            Required = true,
            SortOrder = sort,
            Unit = kind == nameof(ScoringValueKind.Number) ? key == "weight" ? "lb" : "in" : null,
            ChoicesJson = choices,
            CreatedAt = now
        };

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<LeaderboardDbContext> ef)
            throw new SeedException("Leaderboard OnClear requires EfSeedTransport<LeaderboardDbContext>.");

        var db = ef.Context;
        await db.EntryValues.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Adjustments.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Standings.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Entries.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Invites.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Members.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Properties.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Boards.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}