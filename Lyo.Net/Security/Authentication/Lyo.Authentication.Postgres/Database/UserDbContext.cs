using Microsoft.EntityFrameworkCore;

namespace Lyo.Authentication.Postgres.Database;

/// <summary>The EF DbContext that owns the <c>[user]</c> schema for tokens, users, and linked identities.</summary>
public class UserDbContext : DbContext
{
    /// <summary>Lyo users.</summary>
    public DbSet<UserEntity> Users { get; set; } = null!;

    /// <summary>Opaque API tokens (Format B).</summary>
    public DbSet<TokenEntity> Tokens { get; set; } = null!;

    /// <summary>External OIDC identity links.</summary>
    public DbSet<LinkedIdentityEntity> LinkedIdentities { get; set; } = null!;

    /// <summary>Auth audit events.</summary>
    public DbSet<UserEventEntity> Events { get; set; } = null!;

    /// <inheritdoc />
    public UserDbContext(DbContextOptions<UserDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresUserOptions.Schema);
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TokenEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LinkedIdentityEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserEventEntityConfiguration());
    }
}