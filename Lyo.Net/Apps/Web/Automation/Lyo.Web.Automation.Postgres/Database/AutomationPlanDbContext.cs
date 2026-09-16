using Microsoft.EntityFrameworkCore;

namespace Lyo.Web.Automation.Postgres.Database;

/// <summary>Plan rows in the <c>automation</c> schema.</summary>
public sealed class AutomationPlanDbContext : DbContext
{
    /// <summary>Creates the context.</summary>
    public AutomationPlanDbContext(DbContextOptions<AutomationPlanDbContext> options)
        : base(options) { }

    /// <summary>Stored plans.</summary>
    public DbSet<AutomationPlanEntity> Plans => Set<AutomationPlanEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresAutomationPlanOptions.Schema);
        modelBuilder.ApplyConfiguration(new AutomationPlanEntityConfiguration());
    }
}
