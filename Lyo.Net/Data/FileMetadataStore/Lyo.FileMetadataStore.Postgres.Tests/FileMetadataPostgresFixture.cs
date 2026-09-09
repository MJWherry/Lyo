using Lyo.FileMetadataStore.Postgres.Database;
using Lyo.Lock;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileMetadataStore.Postgres.Tests;

public sealed class FileMetadataPostgresFixture : PostgresServiceFixtureBase<FileMetadataStoreDbContext>
{
    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddFileMetadataStoreDbContext(connectionString);
        services.AddLocalLock();
        services.AddPostgresFileDownloadAccessService();
    }

    // The context is registered scoped rather than through IDbContextFactory, so the factory-based default does not apply here.
    protected override async ValueTask MigrateAsync(CancellationToken cancellationToken)
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FileMetadataStoreDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
