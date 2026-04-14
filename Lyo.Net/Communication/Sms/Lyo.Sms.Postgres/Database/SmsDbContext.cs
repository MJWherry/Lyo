using Microsoft.EntityFrameworkCore;

namespace Lyo.Sms.Postgres.Database;

public class SmsDbContext : DbContext
{
    public DbSet<SmsLogEntity> SmsLogs { get; set; } = null!;

    public SmsDbContext(DbContextOptions<SmsDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("sms");
        modelBuilder.ApplyConfiguration(new SmsLogEntityConfiguration());
    }
}