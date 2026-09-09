using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Email.Postgres.Database;

/// <summary>Design-time factory that builds EmailDbContext for migrations.</summary>
public class EmailDbContextFactory : IDesignTimeDbContextFactory<EmailDbContext>
{
    /// <inheritdoc />
    public EmailDbContext CreateDbContext(string[] args)
        => new(
            PostgresDesignTime.CreateOptions<EmailDbContext>(
                "EMAIL_POSTGRES_CONNECTION_STRING", "email", "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres"));
}