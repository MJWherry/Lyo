using Lyo.Common.Core.Identifiers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Story.Sqlite.Tests;

/// <summary>Temp-file SQLite database for the story store, migrated once per test assembly.</summary>
public sealed class StorySqliteFixture : IAsyncLifetime
{
    private string _path = "";

    public ServiceProvider ServiceProvider { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _path = Path.Combine(Path.GetTempPath(), $"lyo-story-{LyoGuid.CreateV7():N}.db");
        var services = new ServiceCollection();
        services.AddSqliteStoryStore(new SqliteStoryOptions { ConnectionString = $"Data Source={_path};Pooling=False" });
        ServiceProvider = services.BuildServiceProvider();
        await ServiceProvider.MigrateSqliteStoryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await ServiceProvider.DisposeAsync();
        if (File.Exists(_path))
            File.Delete(_path);
    }
}
