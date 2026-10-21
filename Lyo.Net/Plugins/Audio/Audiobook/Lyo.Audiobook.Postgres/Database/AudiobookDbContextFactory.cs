using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Audiobook.Postgres.Database;

/// <summary>EF migrations factory for <see cref="AudiobookDbContext" />.</summary>
public class AudiobookDbContextFactory : IDesignTimeDbContextFactory<AudiobookDbContext>
{
    /// <inheritdoc />
    public AudiobookDbContext CreateDbContext(string[] args)
        => new(
            PostgresDesignTime.CreateOptions<AudiobookDbContext>(
                "AUDIOBOOK_CONNECTION_STRING", "audiobook", "Host=127.0.0.1;Port=5432;Database=lyo;Username=postgres;Password=postgres"));
}