using Lyo.Api;
using Lyo.Cache;
using Lyo.Law.Postgres;
using Lyo.Law.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Law.Postgres.Tests;

/// <summary>Testcontainers fixture for the state statute schema.</summary>
public sealed class LawPostgresFixture : PostgresServiceFixtureBase<LawDbContext>
{
    /// <inheritdoc />
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresLawStore(new PostgresLawOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        services.AddLyoCrudServices<LawDbContext>();
    }
}
