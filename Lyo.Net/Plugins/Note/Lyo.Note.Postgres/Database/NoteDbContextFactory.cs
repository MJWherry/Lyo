using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Note.Postgres.Database;

/// <summary>EF migrations factory that builds NoteDbContext instances for migrations.</summary>
public class NoteDbContextFactory : IDesignTimeDbContextFactory<NoteDbContext>
{
    /// <inheritdoc />
    public NoteDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<NoteDbContext>("NOTE_CONNECTION_STRING", "note"));
}