using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Podcast.Postgres.Database;

/// <summary>EF migrations factory for <see cref="PodcastDbContext" />.</summary>
public class PodcastDbContextFactory : IDesignTimeDbContextFactory<PodcastDbContext>
{
    /// <inheritdoc />
    public PodcastDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<PodcastDbContext>(
            "PODCAST_CONNECTION_STRING",
            "podcast",
            "Host=127.0.0.1;Port=5432;Database=lyo;Username=postgres;Password=postgres"));
}
