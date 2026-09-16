using Lyo.Ledger.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Ledger.Postgres.Tests;

/// <summary>Testcontainers fixture for the Ledger schema.</summary>
public sealed class LedgerPostgresFixture : PostgresServiceFixtureBase<LedgerDbContext>
{
    /// <inheritdoc />
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresLedgerStore(new PostgresLedgerOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
