using Lyo.Common.Core.Identifiers;
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

    /// <summary>Current xUnit cancellation token. Reads <see cref="TestContext.Current" /> when accessed, so each test gets that test's token.</summary>
    public CancellationToken TestCt => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"lyo-filemetadata-sqlite-{LyoGuid.CreateV7():N}.db");
        var connectionString = $"Data Source={_dbPath}";
        var services = new ServiceCollection();
        services.AddLogging(b => {
            b.AddConsole();
            b.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddSqliteFileMetadataStoreDbContext(connectionString);
        services.AddLocalLock();
        services.AddSqliteFileDownloadAccessService();
        ServiceProvider = services.BuildServiceProvider();
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SqliteFileMetadataStoreDbContext>();
        await context.Database.MigrateAsync(TestCt);
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