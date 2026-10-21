using Lyo.EntityReference.Models;
using Lyo.Rating.Postgres.Database;
using Lyo.Seed;

namespace Lyo.Rating.Seed.Tests;

public sealed class RatingEfSeedContributorTests
{
    [Fact]
    public async Task RatingEfSeedContributor_Count_PersistsRatings()
    {
        var transport = await SeedAsync(new RatingEfSeedContributor(), 3);
        var rows = transport.Items.OfType<RatingEntity>().ToList();
        var reactions = transport.Items.OfType<RatingReactionEntity>().ToList();
        Assert.Equal(3, rows.Count);
        Assert.Equal(3, reactions.Count);
        var reactionKeys = reactions.Select(row => (row.SubjectEntityType, row.SubjectEntityId, row.ActorEntityType, row.ActorEntityId)).ToList();
        Assert.Equal(reactionKeys.Count, reactionKeys.Distinct().Count());
        Assert.Equal(rows.Count, rows.Select(row => row.SubjectEntityId).Distinct().Count());
        Assert.All(
            rows, row => {
                Assert.Equal(RatingEfSeedContributor.TenantId, row.TenantId);
                Assert.Equal(RatingEfSeedContributor.SubjectType, row.SubjectEntityType);
                Assert.False(string.IsNullOrWhiteSpace(row.SubjectEntityId));
                Assert.Equal(EntityRefVisibility.Public, row.Visibility);
                Assert.False(string.IsNullOrWhiteSpace(row.Subject));
                Assert.False(string.IsNullOrWhiteSpace(row.Title));
                Assert.False(string.IsNullOrWhiteSpace(row.Message));
                Assert.InRange(row.Value!.Value, 1, 5);
                Assert.Contains(
                    reactions,
                    reaction => reaction.TenantId == row.TenantId && reaction.SubjectEntityType == row.SubjectEntityType && reaction.SubjectEntityId == row.SubjectEntityId);
            });
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