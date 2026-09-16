using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Story.Sqlite.Database;

/// <summary>Design-time factory for <see cref="StoryDbContext" /> migrations. Reads <c>STORY_SQLITE_CONNECTION_STRING</c>.</summary>
public sealed class StoryDbContextFactory : IDesignTimeDbContextFactory<StoryDbContext>
{
    /// <inheritdoc />
    public StoryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("STORY_SQLITE_CONNECTION_STRING") ?? "Data Source=./story-design.db";
        return new(new DbContextOptionsBuilder<StoryDbContext>().UseSqlite(connectionString).Options);
    }
}
