using Lyo.Recipe.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Recipe.Postgres.Tests;

/// <summary>Testcontainers fixture for the recipe schema.</summary>
public sealed class RecipePostgresFixture : PostgresServiceFixtureBase<RecipeDbContext>
{
    /// <inheritdoc />
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresRecipeStore(new PostgresRecipeOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
