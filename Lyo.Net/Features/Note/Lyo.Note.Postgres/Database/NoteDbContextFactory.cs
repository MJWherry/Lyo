using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Note.Postgres.Database;

/// <summary>Design-time factory for creating NoteDbContext instances for migrations.</summary>
public class NoteDbContextFactory : IDesignTimeDbContextFactory<NoteDbContext>
{
    public NoteDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("NOTE_CONNECTION_STRING");
        OperationHelpers.ThrowIfNull(connectionString, "NOTE_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<NoteDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "note"));
        return new(optionsBuilder.Options);
    }
}