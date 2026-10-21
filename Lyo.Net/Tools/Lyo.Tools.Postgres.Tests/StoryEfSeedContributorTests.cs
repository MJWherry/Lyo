using Lyo.Seed;
using Lyo.Story.Postgres.Database;
using Lyo.Tools.Postgres.Seeds;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Tools.Postgres.Tests;

public sealed class StoryEfSeedContributorTests
{
    [Fact]
    public async Task StoryEfSeedContributor_Heist_PersistsPlaythrough()
    {
        var transport = await SeedAsync(new StoryEfSeedContributor(NullLogger<StoryEfSeedContributor>.Instance));
        var playthrough = Assert.Single(transport.Items.OfType<StoryPlaythroughEntity>());
        Assert.Equal(StoryEfSeedContributor.SeedStoryId, playthrough.StoryId);
        Assert.Contains(transport.Items.OfType<StoryPlaythroughVariableEntity>(), row => row.PlaythroughId == playthrough.Id && !string.IsNullOrWhiteSpace(row.VariableKey));
        Assert.Contains(transport.Items.OfType<StoryPlaythroughEventEntity>(), row => row.PlaythroughId == playthrough.Id);
    }

    private static async Task<RecordingSeedTransport> SeedAsync(SeedContributor contributor)
    {
        var transport = new RecordingSeedTransport();
        var result = await new SeedRunner().SeedAsync(contributor, transport, new() { Count = 1, RandomSeed = 1 }, TestCt);
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