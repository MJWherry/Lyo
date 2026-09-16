using Lyo.Leaderboard.Postgres;
using Lyo.Leaderboard.Postgres.Database;
using Lyo.Leaderboard.Seed;
using Lyo.Seed;

namespace Lyo.Leaderboard.Seed.Tests;

public sealed class LeaderboardEfSeedContributorTests
{
    [Fact]
    public async Task LeaderboardEfSeedContributor_Count_PersistsBoardGraph()
    {
        var transport = await SeedAsync(new LeaderboardEfSeedContributor(), 2);
        var boards = transport.Items.OfType<BoardEntity>().ToList();
        var properties = transport.Items.OfType<ScoringPropertyEntity>().ToList();
        var members = transport.Items.OfType<MemberEntity>().ToList();
        var entries = transport.Items.OfType<EntryEntity>().ToList();
        var values = transport.Items.OfType<EntryValueEntity>().ToList();
        var standings = transport.Items.OfType<StandingEntity>().ToList();
        Assert.Equal(2, boards.Count);
        Assert.All(boards, board => {
            Assert.Equal(LeaderboardEfSeedContributor.TenantId, board.TenantId);
            Assert.Equal(LeaderboardEfSeedContributor.OwnerUserId, board.OwnerUserId);
            Assert.Equal(LeaderboardCatalog.KeyFor(board.Slug), board.AlgorithmKey);
            Assert.Equal(3, properties.Count(row => row.BoardId == board.Id));
            Assert.Contains(properties, row => row.BoardId == board.Id && row.Key == "fish_type" && row.ChoicesJson != null);
            var boardMembers = members.Where(row => row.BoardId == board.Id).ToList();
            Assert.Equal(3, boardMembers.Count);
            Assert.Contains(transport.Items.OfType<InviteEntity>(), row => row.BoardId == board.Id);
            var boardEntries = entries.Where(row => row.BoardId == board.Id).ToList();
            Assert.Equal(2, boardEntries.Count);
            Assert.All(boardEntries, entry => {
                Assert.Equal(nameof(Lyo.Leaderboard.Enums.EntryStatus.Approved), entry.Status);
                Assert.Equal(LeaderboardEfSeedContributor.BassScore, entry.ComputedScore);
                Assert.Equal(3, values.Count(row => row.EntryId == entry.Id));
                Assert.Contains(values, row => row.EntryId == entry.Id && row.ChoiceKey == "bass");
                Assert.Contains(values, row => row.EntryId == entry.Id && row.NumberValue == 6m);
                Assert.Contains(values, row => row.EntryId == entry.Id && row.NumberValue == 15m);
            });
            Assert.Contains(transport.Items.OfType<AdjustmentEntity>(), row => row.BoardId == board.Id);
            Assert.Equal(3, standings.Count(row => row.BoardId == board.Id));
        });
        var first = Assert.Single(boards, row => row.Slug == "seed-fishing-0");
        Assert.Equal("leaderboard.seed_fishing_0", first.AlgorithmKey);
        var hashes = transport.Items.OfType<InviteEntity>().Select(row => row.TokenHash).ToList();
        Assert.Equal(hashes.Count, hashes.Distinct().Count());
    }

    private static async Task<RecordingSeedTransport> SeedAsync(SeedContributor contributor, int count)
    {
        var transport = new RecordingSeedTransport();
        var result = await new SeedRunner().SeedAsync(contributor, transport, new() { Count = count, RandomSeed = 1 }, TestCt);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        return transport;
    }

    private sealed class RecordingSeedTransport : ISeedTransport
    {
        public List<object> Items { get; } = [];

        public SeedTransportKind Kind => SeedTransportKind.Ef;

        public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
            where T : class
            => Task.FromResult(!Items.OfType<T>().Any());

        public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
            where T : class
        {
            foreach (var item in items)
                Items.Add(item);
            return Task.CompletedTask;
        }
    }
}
