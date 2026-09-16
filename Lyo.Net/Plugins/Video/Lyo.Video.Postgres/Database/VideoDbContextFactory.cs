using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Video.Postgres.Database;

/// <summary>EF migrations factory for <see cref="VideoDbContext" />.</summary>
public class VideoDbContextFactory : IDesignTimeDbContextFactory<VideoDbContext>
{
    /// <inheritdoc />
    public VideoDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<VideoDbContext>(
            "VIDEO_CONNECTION_STRING",
            "video",
            "Host=127.0.0.1;Port=5432;Database=lyo;Username=postgres;Password=postgres"));
}
