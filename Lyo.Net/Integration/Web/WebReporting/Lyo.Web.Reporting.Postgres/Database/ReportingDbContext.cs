using Microsoft.EntityFrameworkCore;

namespace Lyo.Web.Reporting.Postgres.Database;

public class ReportingDbContext : DbContext
{
    public DbSet<ReportEntity> Reports { get; set; } = null!;

    public ReportingDbContext(DbContextOptions<ReportingDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("report");
        modelBuilder.ApplyConfiguration(new ReportEntityConfiguration());
    }
}