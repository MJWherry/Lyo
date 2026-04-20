using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Web.Reporting.Postgres.Database;

public sealed class ReportEntityConfiguration : IEntityTypeConfiguration<ReportEntity>
{
    public void Configure(EntityTypeBuilder<ReportEntity> builder)
    {
        builder.ToTable("reports");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired().HasColumnName("id");
        builder.Property(e => e.Name).HasMaxLength(500).IsRequired().HasColumnName("name");
        builder.Property(e => e.Description).HasMaxLength(2000).HasColumnName("description");
        builder.Property(e => e.ReportDataJson).IsRequired().HasColumnName("report_data_json");
        builder.Property(e => e.ParameterTypeName).HasMaxLength(500).HasColumnName("parameter_type_name");
        builder.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        builder.Property(e => e.UpdatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        builder.Property(e => e.Tags).HasMaxLength(1000).HasColumnName("tags");
        builder.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");

        // Indexes for performance
        builder.HasIndex(e => e.Name).HasDatabaseName("ix_reports_name");
        builder.HasIndex(e => e.CreatedTimestamp).HasDatabaseName("ix_reports_created_timestamp");
        builder.HasIndex(e => e.UpdatedTimestamp).HasDatabaseName("ix_reports_updated_timestamp");
        builder.HasIndex(e => e.IsActive).HasDatabaseName("ix_reports_is_active");
        builder.HasIndex(e => new { e.IsActive, e.Name }).HasDatabaseName("ix_reports_is_active_name");
        builder.HasIndex(e => e.ParameterTypeName).HasDatabaseName("ix_reports_parameter_type_name");
    }
}