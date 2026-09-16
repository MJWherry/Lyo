using Lyo.Seed;
using Lyo.StateMachine.Postgres.Database;
using Lyo.StateMachine.Seed;

namespace Lyo.StateMachine.Seed.Tests;

public sealed class StateMachineEfSeedContributorTests
{
    [Fact]
    public async Task StateMachineEfSeedContributor_Count_PersistsDefinitionInstanceAndTransition()
    {
        var transport = await SeedAsync(new StateMachineEfSeedContributor(), 2);
        var definitions = transport.Items.OfType<StateMachineDefinitionEntity>().ToList();
        Assert.Equal(2, definitions.Count);
        Assert.All(definitions, definition => {
            Assert.Contains("Idle", definition.Document);
            var instance = Assert.Single(transport.Items.OfType<StateMachineInstanceEntity>(), row => row.MachineName == definition.Name);
            Assert.Equal("seed", instance.InstanceKey);
            var transition = Assert.Single(transport.Items.OfType<StateMachineTransitionEntity>(), row => row.MachineName == definition.Name);
            Assert.Equal(instance.InstanceKey, transition.InstanceKey);
            Assert.Equal("Idle", transition.FromState);
            Assert.Equal("Running", transition.ToState);
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
