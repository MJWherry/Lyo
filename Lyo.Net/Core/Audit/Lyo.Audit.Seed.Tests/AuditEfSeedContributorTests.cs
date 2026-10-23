using Lyo.Audit.Postgres.Database;
using Lyo.Seed;

namespace Lyo.Audit.Seed.Tests;

public sealed class AuditEfSeedContributorTests
{
    [Fact]
    public async Task AuditEfSeedContributor_Count_PersistsEventAndChange()
    {
        var transport = await SeedAsync(new AuditEfSeedContributor(), 2);
        var events = transport.Items.OfType<AuditEventEntity>().ToList();
        var changes = transport.Items.OfType<AuditChangeEntity>().ToList();
        Assert.Equal(2, events.Count);
        Assert.Equal(2, changes.Count);
        Assert.All(events, auditEvent => Assert.Contains(changes, row => row.SubjectEntityId == auditEvent.Id.ToString()));
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