using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Tag.Postgres.Database;

/// <summary>EF migrations factory that builds TagDbContext instances for migrations.</summary>
public class TagDbContextFactory : IDesignTimeDbContextFactory<TagDbContext>
{
    /// <inheritdoc />
    public TagDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<TagDbContext>("TAG_CONNECTION_STRING", "tag"));
}