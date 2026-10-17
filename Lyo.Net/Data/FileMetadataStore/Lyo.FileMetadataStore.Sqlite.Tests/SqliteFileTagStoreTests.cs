using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Sqlite.Database;
using Lyo.FileMetadataStore.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileMetadataStore.Sqlite.Tests;

public sealed class SqliteFileTagStoreTests(FileMetadataSqliteFixture fixture) : FileTagStoreContractTests
{
    protected override IServiceProvider Services => fixture.ServiceProvider;

    protected override IFileMetadataStore CreateStore(IServiceScope scope)
        => new SqliteFileMetadataStore(scope.ServiceProvider.GetRequiredService<SqliteFileMetadataStoreDbContext>());
}
