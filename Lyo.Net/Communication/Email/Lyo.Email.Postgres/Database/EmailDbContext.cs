using Microsoft.EntityFrameworkCore;

namespace Lyo.Email.Postgres.Database;

public class EmailDbContext : DbContext
{
    public DbSet<EmailLogEntity> EmailLogs { get; set; } = null!;

    public DbSet<EmailAttachmentLogEntity> EmailAttachmentLogs { get; set; } = null!;

    public EmailDbContext(DbContextOptions<EmailDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("email");
        modelBuilder.ApplyConfiguration(new EmailLogEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EmailAttachmentLogEntityConfiguration());
    }

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added) {
                if (entry.Entity is EmailLogEntity log && log.CreatedTimestamp == default)
                    log.CreatedTimestamp = now;
                else if (entry.Entity is EmailAttachmentLogEntity att && att.CreatedTimestamp == default)
                    att.CreatedTimestamp = now;
            }
            else if (entry.State == EntityState.Modified) {
                if (entry.Entity is EmailLogEntity log)
                    log.UpdatedTimestamp = now;
                else if (entry.Entity is EmailAttachmentLogEntity att)
                    att.UpdatedTimestamp = now;
            }
        }

        return base.SaveChanges();
    }
}