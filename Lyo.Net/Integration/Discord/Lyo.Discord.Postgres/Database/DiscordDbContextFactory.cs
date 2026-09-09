using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Discord.Postgres.Database;

/// <summary>EF migrations factory for EF Core migrations.</summary>
public sealed class DiscordDbContextFactory : IDesignTimeDbContextFactory<DiscordDbContext>
{
    /// <inheritdoc />
    public DiscordDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<DiscordDbContext>("DISCORD_CONNECTION_STRING", PostgresDiscordOptions.Schema));
}