using Lyo.Testing.Containers;
using Lyo.Validation.Postgres.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Validation.Postgres.Tests;

/// <summary>Shared Postgres container with validation migrations already applied.</summary>
public sealed class ValidationPostgresFixture : PostgresServiceFixtureBase<ValidationDbContext>
{
    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, string connectionString) =>
        services.AddPostgresValidationStore(new PostgresValidationOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
