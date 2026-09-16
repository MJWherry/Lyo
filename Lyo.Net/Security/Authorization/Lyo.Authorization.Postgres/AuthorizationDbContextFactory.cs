using Lyo.Authorization.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Authorization.Postgres;

/// <summary>Design-time factory for <c>dotnet ef migrations add</c>.</summary>
public sealed class AuthorizationDbContextFactory : IDesignTimeDbContextFactory<AuthorizationDbContext>
{
    /// <inheritdoc />
    public AuthorizationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuthorizationDbContext>().UseNpgsql(
            "Host=localhost;Database=authorization",
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PostgresAuthorizationOptions.Schema));
        return new AuthorizationDbContext(options.Options);
    }
}
