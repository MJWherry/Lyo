using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Rating.Postgres.Database;

/// <summary>Design-time factory for creating RatingDbContext instances for migrations.</summary>
public class RatingDbContextFactory : IDesignTimeDbContextFactory<RatingDbContext>
{
    public RatingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("RATING_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "RATING_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<RatingDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "rating"));
        return new(optionsBuilder.Options);
    }
}