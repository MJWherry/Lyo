using Lyo.Diagram.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Diagram.Postgres.Tests;

/// <summary>Shared Postgres container with diagram migrations already applied.</summary>
public sealed class DiagramPostgresFixture : PostgresServiceFixtureBase<DiagramDbContext>
{
    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, string connectionString) =>
        services.AddPostgresDiagramStore(new PostgresDiagramOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
