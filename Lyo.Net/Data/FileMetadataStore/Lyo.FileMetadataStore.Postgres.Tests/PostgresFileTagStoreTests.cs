using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileMetadataStore.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileMetadataStore.Postgres.Tests;

public sealed class PostgresFileTagStoreTests(FileMetadataPostgresFixture fixture) : FileTagStoreContractTests
{
    protected override IServiceProvider Services => fixture.ServiceProvider;

    protected override IFileMetadataStore CreateStore(IServiceScope scope)
        => new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());
}
