using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Favorite.Postgres.Database;

/// <summary>EF migrations factory that builds FavoriteDbContext instances for migrations.</summary>
public class FavoriteDbContextFactory : IDesignTimeDbContextFactory<FavoriteDbContext>
{
    /// <inheritdoc />
    public FavoriteDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<FavoriteDbContext>("FAVORITE_CONNECTION_STRING", "favorite"));
}