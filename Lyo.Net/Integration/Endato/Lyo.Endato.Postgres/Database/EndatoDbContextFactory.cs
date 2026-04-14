using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Endato.Postgres.Database;

/// <summary>Design-time factory for creating EndatoDbContext instances for migrations.</summary>
public class EndatoDbContextFactory : IDesignTimeDbContextFactory<EndatoDbContext>
{
    public EndatoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ENDATO_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "ENDATO_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<EndatoDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "endato"));
        return new(optionsBuilder.Options);
    }
}