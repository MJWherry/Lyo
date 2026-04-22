using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Favorite.Postgres.Database;

/// <summary>Design-time factory for creating FavoriteDbContext instances for migrations.</summary>
public class FavoriteDbContextFactory : IDesignTimeDbContextFactory<FavoriteDbContext>
{
    public FavoriteDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FAVORITE_CONNECTION_STRING");
        OperationHelpers.ThrowIfNull(connectionString, "FAVORITE_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<FavoriteDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "favorite"));
        return new(optionsBuilder.Options);
    }
}
