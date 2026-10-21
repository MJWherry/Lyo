using Lyo.Comment.Postgres.Database;
using Lyo.EntityReference.Models;
using Lyo.Seed;

namespace Lyo.Comment.Seed.Tests;

public sealed class CommentEfSeedContributorTests
{
    [Fact]
    public async Task CommentEfSeedContributor_Count_PersistsComments()
    {
        var transport = await SeedAsync(new CommentEfSeedContributor(), 3);
        var rows = transport.Items.OfType<CommentEntity>().ToList();
        var reactions = transport.Items.OfType<CommentReactionEntity>().ToList();
        Assert.Equal(3, rows.Count);
        Assert.Equal(3, reactions.Count);
        var reactionKeys = reactions.Select(row => (row.SubjectEntityType, row.SubjectEntityId, row.ActorEntityType, row.ActorEntityId)).ToList();
        Assert.Equal(reactionKeys.Count, reactionKeys.Distinct().Count());
        Assert.Equal(rows.Count, rows.Select(row => row.SubjectEntityId).Distinct().Count());
        Assert.All(
            rows, row => {
                Assert.Equal(CommentEfSeedContributor.TenantId, row.TenantId);
                Assert.Equal(CommentEfSeedContributor.SubjectType, row.SubjectEntityType);
                Assert.False(string.IsNullOrWhiteSpace(row.SubjectEntityId));
                Assert.Equal(EntityRefVisibility.Public, row.Visibility);
                Assert.False(string.IsNullOrWhiteSpace(row.Content));
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