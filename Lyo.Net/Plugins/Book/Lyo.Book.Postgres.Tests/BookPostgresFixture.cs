using Lyo.Book.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Book.Postgres.Tests;

public sealed class BookPostgresFixture : PostgresServiceFixtureBase<BookDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresBookStore(new PostgresBookOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}