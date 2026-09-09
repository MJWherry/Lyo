using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Sqlite.Database;
using Lyo.FileMetadataStore.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileMetadataStore.Sqlite.Tests;

public sealed class SqliteFileMetadataStoreTests(FileMetadataSqliteFixture fixture) : FileMetadataStoreContractTests
{
    protected override IServiceProvider Services => fixture.ServiceProvider;

    protected override IFileMetadataStore CreateStore(IServiceScope scope)
        => new SqliteFileMetadataStore(scope.ServiceProvider.GetRequiredService<SqliteFileMetadataStoreDbContext>());

    protected override DbSet<FileMetadataEntity> Metadata(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<SqliteFileMetadataStoreDbContext>().FileMetadata;

    [Fact]
    public async Task CheckHealthAsync_WhenConnected_ReturnsHealthy()
    {
        Assert.NotNull(Services);
        using var scope = Services.CreateScope();
        var store = (SqliteFileMetadataStore)CreateStore(scope);
        var result = await store.CheckHealthAsync(TestContext.Current.CancellationToken);
        Assert.True(result.IsHealthy);
    }
}
