using Lyo.FileMetadataStore.Sqlite.Database;
using Lyo.Lock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileMetadataStore.Sqlite.Tests;

public sealed class FileMetadataSqliteFixture : IAsyncLifetime
{
    private string _dbPath = null!;

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"lyo-filemetadata-sqlite-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={_dbPath}";
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddDbContext<SqliteFileMetadataStoreDbContext>(opts => opts.UseSqlite(connectionString));
        services.AddLocalLock();
        services.AddSqliteFileDownloadAccessService();
        ServiceProvider = services.BuildServiceProvider();
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SqliteFileMetadataStoreDbContext>();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        if (ServiceProvider is IDisposable d)
            d.Dispose();

        if (File.Exists(_dbPath))
            File.Delete(_dbPath);

        return ValueTask.CompletedTask;
    }
}