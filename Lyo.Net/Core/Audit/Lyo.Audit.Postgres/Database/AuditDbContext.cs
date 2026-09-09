using Microsoft.EntityFrameworkCore;

namespace Lyo.Audit.Postgres.Database;

public class AuditDbContext : DbContext
{
    public DbSet<AuditChangeEntity> AuditChanges { get; set; } = null!;

    public DbSet<AuditEventEntity> AuditEvents { get; set; } = null!;

    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("audit");
        modelBuilder.ApplyConfiguration(new AuditChangeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AuditEventEntityConfiguration());
    }

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added) {
                if (entry.Entity is AuditEventEntity evt && evt.CreatedTimestamp == default)
                    evt.CreatedTimestamp = now;
                else if (entry.Entity is AuditChangeEntity chg && chg.CreatedTimestamp == default)
                    chg.CreatedTimestamp = now;
            }
            else if (entry.State == EntityState.Modified) {
                if (entry.Entity is AuditEventEntity evt)
                    evt.UpdatedTimestamp = now;
                else if (entry.Entity is AuditChangeEntity chg)
                    chg.UpdatedTimestamp = now;
            }
        }

        return base.SaveChanges();
    }
}