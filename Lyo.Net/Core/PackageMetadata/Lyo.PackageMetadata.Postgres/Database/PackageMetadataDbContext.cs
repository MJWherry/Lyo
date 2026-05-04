using Microsoft.EntityFrameworkCore;

namespace Lyo.PackageMetadata.Postgres.Database;

public class PackageMetadataDbContext : DbContext
{
    public DbSet<PackageMetadataEntity> Packages { get; set; } = null!;

    public DbSet<PackageStackPrefixEntity> StackPrefixes { get; set; } = null!;

    public PackageMetadataDbContext(DbContextOptions<PackageMetadataDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresPackageMetadataOptions.Schema);
        modelBuilder.ApplyConfiguration(new PackageMetadataEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PackageStackPrefixEntityConfiguration());
    }
}
