using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Law.Postgres.Database;

/// <summary>Design-time factory for EF Core migrations.</summary>
public class LawDbContextFactory : IDesignTimeDbContextFactory<LawDbContext>
{
    /// <inheritdoc />
    public LawDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<LawDbContext>("LAW_CONNECTION_STRING", PostgresLawOptions.Schema));
}
