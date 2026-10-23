using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Story.Postgres.Database;

/// <summary>EF migrations factory for <see cref="StoryDbContext" />.</summary>
public class StoryDbContextFactory : IDesignTimeDbContextFactory<StoryDbContext>
{
    /// <inheritdoc />
    public StoryDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<StoryDbContext>("STORY_CONNECTION_STRING", "story", "Host=127.0.0.1;Port=5432;Database=lyo;Username=postgres;Password=postgres"));
}