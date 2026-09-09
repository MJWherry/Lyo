using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Comic.Postgres.Database;

/// <summary>EF migrations factory that builds ComicDbContext instances (used by EF migrations tooling).</summary>
public class ComicDbContextFactory : IDesignTimeDbContextFactory<ComicDbContext>
{
    /// <inheritdoc />
    public ComicDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<ComicDbContext>("COMIC_CONNECTION_STRING", "comic"));
}