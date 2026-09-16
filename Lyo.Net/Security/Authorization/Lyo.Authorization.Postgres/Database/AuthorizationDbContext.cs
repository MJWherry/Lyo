using Microsoft.EntityFrameworkCore;

namespace Lyo.Authorization.Postgres.Database;

/// <summary>EF model for the authorization schema.</summary>
public sealed class AuthorizationDbContext : DbContext
{
    /// <summary>Builds a context.</summary>
    public AuthorizationDbContext(DbContextOptions<AuthorizationDbContext> options)
        : base(options) { }

    /// <summary>Grant rows.</summary>
    public DbSet<GrantEntity> Grants => Set<GrantEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresAuthorizationOptions.Schema);
        modelBuilder.ApplyConfiguration(new GrantEntityConfiguration());
    }
}
