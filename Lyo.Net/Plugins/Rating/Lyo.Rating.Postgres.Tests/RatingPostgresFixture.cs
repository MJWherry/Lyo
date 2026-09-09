using Lyo.Rating.Postgres;
using Lyo.Rating.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Rating.Postgres.Tests;

public sealed class RatingPostgresFixture : PostgresServiceFixtureBase<RatingDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresRatingStore(new PostgresRatingOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
