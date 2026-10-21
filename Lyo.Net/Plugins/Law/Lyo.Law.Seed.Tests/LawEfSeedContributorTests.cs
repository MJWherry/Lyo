using Lyo.Law.Models.Enums;
using Lyo.Law.Postgres.Database;
using Lyo.Seed;

namespace Lyo.Law.Seed.Tests;

public sealed class LawEfSeedContributorTests
{
    [Fact]
    public async Task LawEfSeedContributor_Count_PersistsCodeTree()
    {
        var transport = await SeedAsync(new LawEfSeedContributor(), 2);
        var jurisdictions = transport.Items.OfType<LawJurisdictionEntity>().ToList();
        var codes = transport.Items.OfType<LawCodeEntity>().ToList();
        var nodes = transport.Items.OfType<LawNodeEntity>().ToList();
        var annotations = transport.Items.OfType<LawAnnotationEntity>().ToList();
        var revisions = transport.Items.OfType<LawRevisionEntity>().ToList();
        Assert.Equal(2, jurisdictions.Count);
        Assert.Equal(2, codes.Count);
        Assert.All(jurisdictions, row => Assert.StartsWith("US-", row.Key));
        Assert.All(
            codes, code => {
                var title = Assert.Single(nodes, row => row.CodeId == code.Id && row.Kind == LawNodeKind.Title && row.ParentId == null);
                var chapter = Assert.Single(nodes, row => row.ParentId == title.Id && row.Kind == LawNodeKind.Chapter);
                var section = Assert.Single(nodes, row => row.ParentId == chapter.Id && row.Kind == LawNodeKind.Section);
                Assert.False(string.IsNullOrWhiteSpace(section.Body));
                Assert.False(string.IsNullOrWhiteSpace(section.SourceKey));
                Assert.Equal(64, section.ContentSha256.Length);
                Assert.Contains(annotations, row => row.NodeId == section.Id);
                var revision = Assert.Single(revisions, row => row.NodeId == section.Id);
                Assert.Equal(section.Heading, revision.Heading);
                Assert.Equal(section.Body, revision.Body);
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