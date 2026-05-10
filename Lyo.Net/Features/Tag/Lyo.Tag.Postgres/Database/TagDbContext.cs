using Lyo.EntityReference.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Tag.Postgres.Database;

public class TagDbContext : EntityRefModuleDbContext
{
    public DbSet<TagEntity> Tags { get; set; } = null!;

    public TagDbContext(DbContextOptions<TagDbContext> options)
        : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tag");
        modelBuilder.ApplyConfiguration(new TagEntityConfiguration());
    }
}
