using Lyo.Email.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Email.Postgres.Tests;

/// <summary>Postgres Testcontainers fixture with email schema migrations applied.</summary>
public sealed class EmailPostgresFixture : PostgresServiceFixtureBase<EmailDbContext>
{
    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, string connectionString) =>
        services.AddEmailDbContextFactory(new PostgresEmailOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
