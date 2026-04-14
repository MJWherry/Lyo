using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Tag.Postgres.Database;

/// <summary>Design-time factory for creating TagDbContext instances for migrations.</summary>
public class TagDbContextFactory : IDesignTimeDbContextFactory<TagDbContext>
{
    public TagDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TAG_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "TAG_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<TagDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "tag"));
        return new(optionsBuilder.Options);
    }
}