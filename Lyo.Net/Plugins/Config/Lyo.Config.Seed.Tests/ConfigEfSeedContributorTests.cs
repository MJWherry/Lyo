using Lyo.Config.Postgres.Database;
using Lyo.Seed;

namespace Lyo.Config.Seed.Tests;

public sealed class ConfigEfSeedContributorTests
{
    [Fact]
    public async Task ConfigEfSeedContributor_Count_PersistsDefinitionsAndBindings()
    {
        var transport = await SeedAsync(new ConfigEfSeedContributor(), 3);
        var definitions = transport.Items.OfType<ConfigDefinitionEntity>().ToList();
        var bindings = transport.Items.OfType<ConfigBindingEntity>().ToList();
        var definitionRevisions = transport.Items.OfType<ConfigDefinitionRevisionEntity>().ToList();
        var bindingRevisions = transport.Items.OfType<ConfigBindingRevisionEntity>().ToList();
        Assert.Equal(3, definitions.Count);
        Assert.Equal(3, bindings.Count);
        Assert.Equal(3, definitionRevisions.Count);
        Assert.Equal(3, bindingRevisions.Count);
        Assert.All(
            definitions, definition => {
                Assert.False(definition.IsEncrypted);
                Assert.False(string.IsNullOrWhiteSpace(definition.DefaultValueJson));
                Assert.Contains(bindings, binding => binding.DefinitionId == definition.Id);
                var revision = Assert.Single(definitionRevisions, row => row.DefinitionId == definition.Id);
                Assert.Equal(1, revision.Revision);
                Assert.Equal(definition.Key, revision.Key);
                Assert.Equal(definition.ForValueType, revision.ForValueType);
                Assert.Equal(definition.IsEncrypted, revision.IsEncrypted);
                Assert.Equal(definition.DefaultValueJson, revision.DefaultValueJson);
            });

        Assert.All(
            bindings, binding => {
                var revision = Assert.Single(bindingRevisions, row => row.BindingId == binding.Id);
                Assert.Equal(1, revision.Revision);
                Assert.False(string.IsNullOrWhiteSpace(revision.ValueJson));
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