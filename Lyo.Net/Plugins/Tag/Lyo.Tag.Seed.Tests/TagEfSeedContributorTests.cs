using Lyo.EntityReference.Models;
using Lyo.Seed;
using Lyo.Tag.Postgres.Database;
using Lyo.Tag.Seed;

namespace Lyo.Tag.Seed.Tests;

public sealed class TagEfSeedContributorTests
{
    [Fact]
    public async Task TagEfSeedContributor_Count_PersistsTags()
    {
        var transport = await SeedAsync(new TagEfSeedContributor(), 3);
        var rows = transport.Items.OfType<TagEntity>().ToList();
        Assert.Equal(3, rows.Count);
        Assert.Equal(3, rows.Select(r => r.Slug).Distinct().Count());
        Assert.Equal(3, rows.Select(r => r.SubjectEntityId).Distinct().Count());
        Assert.All(rows, row => {
            Assert.Equal(TagEfSeedContributor.TenantId, row.TenantId);
            Assert.Equal(TagEfSeedContributor.SubjectType, row.SubjectEntityType);
            Assert.False(string.IsNullOrWhiteSpace(row.SubjectEntityId));
            Assert.Equal(EntityRefVisibility.Public, row.Visibility);
            Assert.Equal("tag", row.TagType);
            Assert.False(string.IsNullOrWhiteSpace(row.Name));
            Assert.False(string.IsNullOrWhiteSpace(row.Slug));
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
