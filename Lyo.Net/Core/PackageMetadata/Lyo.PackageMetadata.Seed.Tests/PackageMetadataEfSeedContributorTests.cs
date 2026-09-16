using Lyo.PackageMetadata.Postgres.Database;
using Lyo.PackageMetadata.Seed;
using Lyo.Seed;

namespace Lyo.PackageMetadata.Seed.Tests;

public sealed class PackageMetadataEfSeedContributorTests
{
    [Fact]
    public async Task PackageMetadataEfSeedContributor_Count_PersistsPackageAndPrefix()
    {
        var transport = await SeedAsync(new PackageMetadataEfSeedContributor(), 2);
        var packages = transport.Items.OfType<PackageMetadataEntity>().ToList();
        Assert.Equal(2, packages.Count);
        Assert.All(packages, package => {
            var prefix = Assert.Single(transport.Items.OfType<PackageStackPrefixEntity>(), row => row.PackageMetadataId == package.Id);
            Assert.Equal(package.Name + ".", prefix.NormalizedPrefix);
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
