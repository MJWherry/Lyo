using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Note.Postgres;
using Lyo.Note.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Note.Postgres.Tests;

public sealed class NotePostgresFixture : PostgresServiceFixtureBase<NoteDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddEntityRefOptions();
        services.AddPostgresNoteStore(new PostgresNoteOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
    }
}
