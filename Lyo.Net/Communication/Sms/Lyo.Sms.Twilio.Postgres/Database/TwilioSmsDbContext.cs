using Microsoft.EntityFrameworkCore;

namespace Lyo.Sms.Twilio.Postgres.Database;

public class TwilioSmsDbContext : DbContext
{
    public DbSet<TwilioSmsLogEntity> TwilioSmsLogs { get; set; } = null!;

    public TwilioSmsDbContext(DbContextOptions<TwilioSmsDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("sms");
        modelBuilder.ApplyConfiguration(new TwilioSmsLogEntityConfiguration());
    }

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added && entry.Entity is TwilioSmsLogEntity log && log.CreatedTimestamp == default)
                log.CreatedTimestamp = now;
            else if (entry.State == EntityState.Modified && entry.Entity is TwilioSmsLogEntity log2)
                log2.UpdatedTimestamp = now;
        }

        return base.SaveChanges();
    }
}