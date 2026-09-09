using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Endato.Postgres.Database;

/// <summary>EF migrations factory that builds EndatoDbContext instances for migrations.</summary>
public class EndatoDbContextFactory : IDesignTimeDbContextFactory<EndatoDbContext>
{
    /// <inheritdoc />
    public EndatoDbContext CreateDbContext(string[] args) => new(PostgresDesignTime.CreateOptions<EndatoDbContext>("ENDATO_CONNECTION_STRING", "endato"));
}