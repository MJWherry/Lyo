using Lyo.FileMetadataStore.Models;
using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.FileMetadataStore.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileMetadataStore.Postgres.Tests;

public sealed class PostgresFileMetadataStoreTests(FileMetadataPostgresFixture fixture) : FileMetadataStoreContractTests
{
    protected override IServiceProvider Services => fixture.ServiceProvider;

    protected override IFileMetadataStore CreateStore(IServiceScope scope)
        => new PostgresFileMetadataStore(scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>());

    protected override DbSet<FileMetadataEntity> Metadata(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>().FileMetadata;
}
