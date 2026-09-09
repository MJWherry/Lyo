using Lyo.Tag.Postgres;
using Lyo.Tag.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Tag.Postgres.Tests;

public sealed class TagPostgresFixture : PostgresServiceFixtureBase<TagDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresTagStore(new PostgresTagOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
