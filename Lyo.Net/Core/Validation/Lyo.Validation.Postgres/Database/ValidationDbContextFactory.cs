using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Validation.Postgres.Database;

/// <summary>Design-time factory for EF migrations. Needs <c>VALIDATION_CONNECTION_STRING</c>.</summary>
public sealed class ValidationDbContextFactory : IDesignTimeDbContextFactory<ValidationDbContext>
{
    /// <inheritdoc />
    public ValidationDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<ValidationDbContext>("VALIDATION_CONNECTION_STRING", PostgresValidationOptions.Schema));
}
