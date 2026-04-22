using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Comic.Postgres.Database;

/// <summary>Design-time factory for creating ComicDbContext instances (used by EF migrations tooling).</summary>
public class ComicDbContextFactory : IDesignTimeDbContextFactory<ComicDbContext>
{
    public ComicDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("COMIC_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "COMIC_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<ComicDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "comic"));
        return new(optionsBuilder.Options);
    }
}