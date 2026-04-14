using Microsoft.EntityFrameworkCore;

namespace Lyo.Config.Postgres.Database;

public class ConfigDbContext : DbContext
{
    public DbSet<ConfigDefinitionEntity> ConfigDefinitions { get; set; } = null!;

    public DbSet<ConfigBindingEntity> ConfigBindings { get; set; } = null!;

    public DbSet<ConfigBindingRevisionEntity> ConfigBindingRevisions { get; set; } = null!;

    public ConfigDbContext(DbContextOptions<ConfigDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("config");
        modelBuilder.ApplyConfiguration(new ConfigDefinitionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ConfigBindingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ConfigBindingRevisionEntityConfiguration());
    }

    public override int SaveChanges()
    {
        StampAuditColumns();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditColumns();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAuditColumns()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added) {
                if (entry.Entity is ConfigDefinitionEntity definition && definition.CreatedTimestamp == default)
                    definition.CreatedTimestamp = now;
                else if (entry.Entity is ConfigBindingEntity binding && binding.CreatedTimestamp == default)
                    binding.CreatedTimestamp = now;
            }
            else if (entry.State == EntityState.Modified) {
                if (entry.Entity is ConfigDefinitionEntity definition)
                    definition.UpdatedTimestamp = now;
                else if (entry.Entity is ConfigBindingEntity binding)
                    binding.UpdatedTimestamp = now;
            }
        }
    }
}