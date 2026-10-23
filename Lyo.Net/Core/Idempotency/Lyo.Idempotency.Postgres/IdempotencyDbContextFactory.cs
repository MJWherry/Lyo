using Lyo.Idempotency.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Idempotency.Postgres;

/// <summary>Design-time factory so <c>dotnet ef migrations add</c> can build the model.</summary>
public sealed class IdempotencyDbContextFactory : IDesignTimeDbContextFactory<IdempotencyDbContext>
{
    /// <inheritdoc />
    public IdempotencyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdempotencyDbContext>().UseNpgsql(
            "Host=localhost;Database=idempotency", npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PostgresIdempotencyOptions.Schema));

        return new(options.Options);
    }
}