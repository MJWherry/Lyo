using Lyo.Email.Postgres.Database;
using Lyo.Email.Seed;
using Lyo.Seed;

namespace Lyo.Email.Seed.Tests;

public sealed class EmailEfSeedContributorTests
{
    [Fact]
    public async Task EmailEfSeedContributor_Count_PersistsLogAndAttachment()
    {
        var transport = await SeedAsync(new EmailEfSeedContributor(), 2);
        var logs = transport.Items.OfType<EmailLogEntity>().ToList();
        Assert.Equal(2, logs.Count);
        Assert.All(logs, log => Assert.Contains(transport.Items.OfType<EmailAttachmentLogEntity>(), row => row.EmailLogId == log.Id));
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
        public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default) where T : class => Task.FromResult(!Items.OfType<T>().Any());
        public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default) where T : class
        {
            foreach (var item in items)
                Items.Add(item);
            return Task.CompletedTask;
        }
    }
}
