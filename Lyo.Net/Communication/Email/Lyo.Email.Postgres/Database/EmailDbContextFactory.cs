using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Email.Postgres.Database;

/// <summary>Design-time factory for creating EmailDbContext instances for migrations.</summary>
public class EmailDbContextFactory : IDesignTimeDbContextFactory<EmailDbContext>
{
    public EmailDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("EMAIL_POSTGRES_CONNECTION_STRING") ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<EmailDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "email"));
        return new(optionsBuilder.Options);
    }
}