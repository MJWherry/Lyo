using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Web.Automation.Postgres.Database;

/// <summary>Maps <see cref="AutomationPlanEntity" /> to <c>automation.automation_plans</c>.</summary>
public sealed class AutomationPlanEntityConfiguration : IEntityTypeConfiguration<AutomationPlanEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AutomationPlanEntity> builder)
    {
        builder.ToTable("automation_plans");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Document).HasColumnName("document").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.StepCount).HasColumnName("step_count");
        builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc").HasColumnType("timestamp with time zone");
        builder.HasIndex(e => e.Name).IsUnique().HasDatabaseName("ux_automation_plans_name");
    }
}
