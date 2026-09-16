using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Music.Postgres.Database;

/// <summary>EF migrations factory for <see cref="MusicDbContext" />.</summary>
public class MusicDbContextFactory : IDesignTimeDbContextFactory<MusicDbContext>
{
    /// <inheritdoc />
    public MusicDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<MusicDbContext>(
            "MUSIC_CONNECTION_STRING",
            "music",
            "Host=127.0.0.1;Port=5432;Database=lyo;Username=postgres;Password=postgres"));
}
