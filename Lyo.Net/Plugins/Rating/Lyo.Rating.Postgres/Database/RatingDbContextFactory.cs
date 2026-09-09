using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Rating.Postgres.Database;

/// <summary>EF migrations factory that builds RatingDbContext instances for migrations.</summary>
public class RatingDbContextFactory : IDesignTimeDbContextFactory<RatingDbContext>
{
    /// <inheritdoc />
    public RatingDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<RatingDbContext>("RATING_CONNECTION_STRING", "rating"));
}