using Microsoft.EntityFrameworkCore;

namespace Lyo.Reporting.Postgres.Database;

public sealed class ReportingContext : DbContext
{
    public DbSet<ReportDefinition> ReportDefinitions { get; set; } = null!;

    public DbSet<ReportDefinitionParameter> ReportDefinitionParameters { get; set; } = null!;

    public DbSet<ReportGeneration> ReportGenerations { get; set; } = null!;

    public DbSet<ReportGenerationParameter> ReportGenerationParameters { get; set; } = null!;

    public ReportingContext(DbContextOptions<ReportingContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresReportingOptions.Schema);

        modelBuilder.Entity<ReportDefinition>(entity => {
            entity.ToTable("report_definition");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(500).IsRequired().HasColumnName("name");
            entity.Property(e => e.Description).HasMaxLength(2000).HasColumnName("description");
            entity.Property(e => e.ReportDataJson).IsRequired().HasColumnType("text").HasColumnName("report_data_json");
            entity.Property(e => e.Tags).HasMaxLength(1000).HasColumnName("tags");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.DefaultFormat).HasMaxLength(32).HasColumnName("default_format");
            entity.Property(e => e.DefaultFileName).HasMaxLength(500).HasColumnName("default_file_name");
            entity.Property(e => e.DefaultPathPrefix).HasMaxLength(500).HasColumnName("default_path_prefix");
            entity.Property(e => e.GenerationProfileKey).HasMaxLength(200).HasColumnName("generation_profile_key");
            entity.Property(e => e.CreatedBy).HasMaxLength(500).HasColumnName("created_by");
            entity.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_report_definition_name");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("ix_report_definition_is_active");
            entity.HasIndex(e => e.CreatedTimestamp).HasDatabaseName("ix_report_definition_created_timestamp");
            entity.HasIndex(e => e.GenerationProfileKey).HasDatabaseName("ix_report_definition_generation_profile_key");
        });

        modelBuilder.Entity<ReportDefinitionParameter>(entity => {
            entity.ToTable("report_definition_parameter");
            entity.HasKey(e => e.Id).HasName("pk_report_definition_parameter");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ReportDefinitionId).HasColumnName("report_definition_id");
            entity.Property(e => e.Description).HasMaxLength(100).HasColumnName("description");
            entity.Property(e => e.Key).HasMaxLength(50).IsRequired().HasColumnName("key");
            entity.Property(e => e.Type).HasMaxLength(15).IsRequired().HasColumnName("type");
            entity.Property(e => e.Value).HasMaxLength(3000).HasColumnName("value");
            entity.Property(e => e.EncryptedValue).HasColumnName("encrypted_value");
            entity.Property(e => e.AllowMultiple).HasColumnName("allow_multiple");
            entity.Property(e => e.Required).HasDefaultValue(true).HasColumnName("required");
            entity.Property(e => e.ValidationRegex).HasMaxLength(500).HasColumnName("validation_regex");
            entity.Property(e => e.MinLength).HasColumnName("min_length");
            entity.Property(e => e.MaxLength).HasColumnName("max_length");
            entity.Property(e => e.AllowedValues).HasMaxLength(1000).HasColumnName("allowed_values");
            entity.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.HasIndex(e => e.ReportDefinitionId).HasDatabaseName("ix_report_definition_parameter_definition_id");
            entity.HasIndex(e => new { e.ReportDefinitionId, e.Key }).IsUnique().HasDatabaseName("ix_report_definition_parameter_definition_key");
            entity.HasOne(e => e.ReportDefinition)
                .WithMany(d => d.Parameters)
                .HasForeignKey(e => e.ReportDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReportGeneration>(entity => {
            entity.ToTable("report_generation");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ReportDefinitionId).HasColumnName("report_definition_id");
            entity.Property(e => e.ReportDataJson).IsRequired().HasColumnType("text").HasColumnName("report_data_json");
            entity.Property(e => e.Format).HasMaxLength(32).IsRequired().HasColumnName("format");
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired().HasColumnName("status");
            entity.Property(e => e.OutputFileId).HasColumnName("output_file_id");
            entity.Property(e => e.OriginalFileName).HasMaxLength(500).HasColumnName("original_file_name");
            entity.Property(e => e.ContentType).HasMaxLength(200).HasColumnName("content_type");
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000).HasColumnName("error_message");
            entity.Property(e => e.PathPrefix).HasMaxLength(500).HasColumnName("path_prefix");
            entity.Property(e => e.CreatedBy).HasMaxLength(50).IsRequired().HasColumnName("created_by");
            entity.Property(e => e.CreatedTimestamp).IsRequired().HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.StartedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("started_timestamp");
            entity.Property(e => e.FinishedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("finished_timestamp");
            entity.HasIndex(e => e.ReportDefinitionId).HasDatabaseName("ix_report_generation_definition_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_report_generation_status");
            entity.HasIndex(e => e.CreatedTimestamp).HasDatabaseName("ix_report_generation_created_timestamp");
            entity.HasIndex(e => e.OutputFileId).HasDatabaseName("ix_report_generation_output_file_id");
            entity.HasOne(e => e.ReportDefinition)
                .WithMany(d => d.Generations)
                .HasForeignKey(e => e.ReportDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReportGenerationParameter>(entity => {
            entity.ToTable("report_generation_parameter");
            entity.HasKey(e => e.Id).HasName("pk_report_generation_parameter");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ReportGenerationId).HasColumnName("report_generation_id");
            entity.Property(e => e.Description).HasMaxLength(100).HasColumnName("description");
            entity.Property(e => e.Key).HasMaxLength(50).IsRequired().HasColumnName("key");
            entity.Property(e => e.Type).HasMaxLength(15).IsRequired().HasColumnName("type");
            entity.Property(e => e.Value).HasMaxLength(3000).HasColumnName("value");
            entity.Property(e => e.EncryptedValue).HasColumnName("encrypted_value");
            entity.HasIndex(e => e.ReportGenerationId).HasDatabaseName("ix_report_generation_parameter_generation_id");
            entity.HasIndex(e => new { e.ReportGenerationId, e.Key }).HasDatabaseName("ix_report_generation_parameter_generation_key");
            entity.HasOne(e => e.ReportGeneration)
                .WithMany(g => g.Parameters)
                .HasForeignKey(e => e.ReportGenerationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
