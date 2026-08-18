using Lyo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Validation.Postgres.Database;

/// <summary>Design-time factory for EF migrations. Requires <c>VALIDATION_CONNECTION_STRING</c>.</summary>
public sealed class ValidationDbContextFactory : IDesignTimeDbContextFactory<ValidationDbContext>
{
    /// <inheritdoc />
    public ValidationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("VALIDATION_CONNECTION_STRING");
        OperationHelpers.ThrowIfNullOrWhiteSpace(connectionString, "VALIDATION_CONNECTION_STRING environment variable must be set for design-time operations.");
        var optionsBuilder = new DbContextOptionsBuilder<ValidationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresValidationOptions.Schema));
        return new(optionsBuilder.Options);
    }
}
