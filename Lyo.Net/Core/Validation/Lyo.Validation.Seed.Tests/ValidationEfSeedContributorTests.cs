using Lyo.Seed;
using Lyo.Validation.Postgres.Database;
using Lyo.Validation.Seed;

namespace Lyo.Validation.Seed.Tests;

public sealed class ValidationEfSeedContributorTests
{
    [Fact]
    public async Task ValidationEfSeedContributor_Count_PersistsSchemas()
    {
        var transport = await SeedAsync(new ValidationEfSeedContributor(), 2);
        var schemas = transport.Items.OfType<ValidationSchemaEntity>().ToList();
        Assert.Equal(2, schemas.Count);
        Assert.All(schemas, schema => {
            Assert.StartsWith("contact.schema.", schema.Key);
            Assert.Contains("required", schema.ConstraintsJson);
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
