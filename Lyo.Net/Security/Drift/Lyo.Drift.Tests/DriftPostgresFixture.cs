using Lyo.Api;
using Lyo.Cache;
using Lyo.Drift.Postgres;
using Lyo.Drift.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Drift.Tests;

public sealed class DriftPostgresFixture : PostgresServiceFixtureBase<DriftDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresDriftManagement(new PostgresDriftOptions {
            ConnectionString = connectionString,
            EnableAutoMigrations = true,
            MaxSnapshotJsonBytes = 8 * 1024 * 1024,
            SnapshotRetention = TimeSpan.FromDays(1)
        });
    }
}
