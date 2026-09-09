using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Drift.Postgres.Database;

/// <summary>EF migrations factory for design-time <c>dotnet ef</c>.</summary>
public sealed class DriftDbContextFactory : IDesignTimeDbContextFactory<DriftDbContext>
{
    /// <inheritdoc />
    public DriftDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<DriftDbContext>(
            "DRIFT_CONNECTION_STRING", PostgresDriftOptions.Schema, "Host=localhost;Database=lyo;Username=lyo;Password=lyo"));
}
