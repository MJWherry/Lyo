using Lyo.Comment.Postgres;
using Lyo.Comment.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Comment.Postgres.Tests;

public sealed class CommentPostgresFixture : PostgresServiceFixtureBase<CommentDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresCommentStore(new PostgresCommentOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
