using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Discord.Postgres.Database;

/// <summary>Design-time factory for EF Core migrations.</summary>
public sealed class DiscordDbContextFactory : IDesignTimeDbContextFactory<DiscordDbContext>
{
    public DiscordDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DISCORD_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "DISCORD_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<DiscordDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PostgresDiscordOptions.Schema));
        return new(optionsBuilder.Options);
    }
}