using Lyo.Algorithms.Sqlite.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Algorithms.Sqlite.Tests;

public sealed class AlgorithmSqliteFixture : IAsyncLifetime
{
    private string _dbPath = "";

    public ServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>Current xUnit cancellation token. Reads <see cref="TestContext.Current"/> when accessed, so each test gets that test's token.</summary>
    public CancellationToken TestCt => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"lyo-algorithms-{LyoGuid.CreateV7():N}.db");
        var services = new ServiceCollection();
        services.AddAlgorithms();
        services.AddSqliteAlgorithmStore(new SqliteAlgorithmOptions { ConnectionString = $"Data Source={_dbPath}" });
        ServiceProvider = services.BuildServiceProvider();
        await using var context = await ServiceProvider.GetRequiredService<IDbContextFactory<AlgorithmDbContext>>().CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await ServiceProvider.DisposeAsync();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
