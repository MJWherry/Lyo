using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Algorithms.Postgres.Database;

/// <summary>Design-time factory for <see cref="AlgorithmDbContext" /> migrations.</summary>
public sealed class AlgorithmDbContextFactory : IDesignTimeDbContextFactory<AlgorithmDbContext>
{
    /// <inheritdoc />
    public AlgorithmDbContext CreateDbContext(string[] args)
        => new(
            PostgresDesignTime.CreateOptions<AlgorithmDbContext>(
                "ALGORITHMS_CONNECTION_STRING", PostgresAlgorithmOptions.Schema, "Host=127.0.0.1;Port=5432;Database=lyo;Username=postgres;Password=postgres"));
}