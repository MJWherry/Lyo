using Microsoft.EntityFrameworkCore;

namespace Lyo.Job.Postgres.Database;

public partial class JobContext : DbContext
{
    public virtual DbSet<JobDefinition> JobDefinitions { get; set; }

    public virtual DbSet<JobFileUpload> JobFileUploads { get; set; }

    public virtual DbSet<JobParallelRestriction> JobParallelRestrictions { get; set; }

    public virtual DbSet<JobParameter> JobParameters { get; set; }

    public virtual DbSet<JobRun> JobRuns { get; set; }

    public virtual DbSet<JobRunLog> JobRunLogs { get; set; }

    public virtual DbSet<JobRunParameter> JobRunParameters { get; set; }

    public virtual DbSet<JobRunResult> JobRunResults { get; set; }

    public virtual DbSet<JobSchedule> JobSchedules { get; set; }

    public virtual DbSet<JobScheduleParameter> JobScheduleParameters { get; set; }

    public virtual DbSet<JobTrigger> JobTriggers { get; set; }

    public virtual DbSet<JobTriggerParameter> JobTriggerParameters { get; set; }

    public JobContext(DbContextOptions<JobContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("job");
        modelBuilder.Entity<JobDefinition>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_definition");
            entity.ToTable("job_definition");
            entity.HasIndex(e => e.Name, "ix_job_definition_name");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Description).HasMaxLength(100).HasColumnName("description");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Type).HasMaxLength(25).HasColumnName("type");
            entity.Property(e => e.WorkerType).HasMaxLength(7).HasColumnName("worker_type");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        });

        modelBuilder.Entity<JobFileUpload>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_file_upload");
            entity.ToTable("job_file_upload");
            entity.HasIndex(e => e.OriginalHash, "ix_job_file_upload_original_hash");
            entity.HasIndex(e => e.SourceHash, "ix_job_file_upload_source_hash");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.DataEncryptionKeyVersion).HasColumnName("data_encryption_key_version");
            entity.Property(e => e.EncryptedDataEncryptionKey).HasColumnName("encrypted_data_encryption_key");
            entity.Property(e => e.OriginalFilename).HasMaxLength(100).HasColumnName("original_filename");
            entity.Property(e => e.OriginalHash).HasColumnName("original_hash");
            entity.Property(e => e.OriginalSize).HasColumnName("original_size");
            entity.Property(e => e.SourceDirectory).HasMaxLength(150).HasColumnName("source_directory");
            entity.Property(e => e.SourceFilename).HasMaxLength(50).HasColumnName("source_filename");
            entity.Property(e => e.SourceHash).HasColumnName("source_hash");
            entity.Property(e => e.SourceSize).HasColumnName("source_size");
            entity.Property(e => e.UploadTimestamp).HasColumnType("timestamp with time zone").HasColumnName("upload_timestamp");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        });

        modelBuilder.Entity<JobParallelRestriction>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_parallel_restriction");
            entity.ToTable("job_parallel_restriction");
            entity.HasIndex(e => e.BaseJobDefinitionId, "ix_job_parallel_restriction_base_job_definition_id");
            entity.HasIndex(e => e.OtherJobDefinitionId, "ix_job_parallel_restriction_other_job_definition_id");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.BaseJobDefinitionId).HasColumnName("base_job_definition_id");
            entity.Property(e => e.Description).HasMaxLength(100).HasColumnName("description");
            entity.Property(e => e.Enabled).HasDefaultValue(true).HasColumnName("enabled");
            entity.Property(e => e.OtherJobDefinitionId).HasColumnName("other_job_definition_id");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.HasOne(d => d.BaseJobDefinition)
                .WithMany(p => p.JobParallelRestrictionBaseJobDefinitions)
                .HasForeignKey(d => d.BaseJobDefinitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_parallel_restriction_base");

            entity.HasOne(d => d.OtherJobDefinition)
                .WithMany(p => p.JobParallelRestrictionOtherJobDefinitions)
                .HasForeignKey(d => d.OtherJobDefinitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_parallel_restriction_other");
        });

        modelBuilder.Entity<JobParameter>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_parameter");
            entity.ToTable("job_parameter");
            entity.HasIndex(e => e.JobDefinitionId, "ix_job_parameter_job_definition_id");
            entity.HasIndex(e => e.Key, "ix_job_parameter_key");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.AllowMultiple).HasDefaultValue(false).HasColumnName("allow_multiple");
            entity.Property(e => e.Description).HasMaxLength(100).HasColumnName("description");
            entity.Property(e => e.EncryptedValue).HasColumnName("encrypted_value");
            entity.Property(e => e.JobDefinitionId).HasColumnName("job_definition_id");
            entity.Property(e => e.Key).HasMaxLength(50).HasColumnName("key");
            entity.Property(e => e.Required).HasDefaultValue(true).HasColumnName("required");
            entity.Property(e => e.Type).HasMaxLength(15).HasColumnName("type");
            entity.Property(e => e.Value).HasMaxLength(300).HasColumnName("value");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.HasOne(d => d.JobDefinition)
                .WithMany(p => p.JobParameters)
                .HasForeignKey(d => d.JobDefinitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_parameter_job_definition_job_definition_id");
        });

        modelBuilder.Entity<JobRun>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_run");
            entity.ToTable("job_run");
            entity.HasIndex(e => e.JobDefinitionId, "ix_job_run_job_definition_id");
            entity.HasIndex(e => e.JobScheduleId, "ix_job_run_job_schedule_id");
            entity.HasIndex(e => e.JobTriggerId, "ix_job_run_job_trigger_id");
            entity.HasIndex(e => e.State, "ix_job_run_state");
            entity.HasIndex(e => e.TriggeredByJobRunId, "ix_job_run_triggered_by_job_run_id");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.AllowTriggers).HasColumnName("allow_triggers");
            entity.Property(e => e.CreatedBy).HasMaxLength(50).HasColumnName("created_by");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.FinishedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("finished_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.Property(e => e.JobDefinitionId).HasColumnName("job_definition_id");
            entity.Property(e => e.JobScheduleId).HasColumnName("job_schedule_id");
            entity.Property(e => e.JobTriggerId).HasColumnName("job_trigger_id");
            entity.Property(e => e.ReRanFromJobRunId).HasColumnName("re_ran_from_job_run_id");
            entity.Property(e => e.Result).HasMaxLength(20).HasColumnName("result");
            entity.Property(e => e.StartedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("started_timestamp");
            entity.Property(e => e.State).HasMaxLength(12).HasColumnName("state");
            entity.Property(e => e.TriggeredByJobRunId).HasColumnName("triggered_by_job_run_id");
            entity.HasOne(d => d.JobDefinition)
                .WithMany(p => p.JobRuns)
                .HasForeignKey(d => d.JobDefinitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_run_job_definition_job_definition_id");

            entity.HasOne(d => d.JobSchedule).WithMany(p => p.JobRuns).HasForeignKey(d => d.JobScheduleId).HasConstraintName("fk_job_run_job_schedule_job_schedule_id");
            entity.HasOne(d => d.JobTrigger).WithMany(p => p.JobRuns).HasForeignKey(d => d.JobTriggerId).HasConstraintName("fk_job_run_job_trigger_job_trigger_id");
            entity.HasOne(d => d.ReRanFromJobRun).WithMany(p => p.InverseReRanFromJobRun).HasForeignKey(d => d.ReRanFromJobRunId).HasConstraintName("fk_job_run_re_ran_from");
            entity.HasOne(d => d.TriggeredByJobRun)
                .WithMany(p => p.InverseTriggeredByJobRun)
                .HasForeignKey(d => d.TriggeredByJobRunId)
                .HasConstraintName("fk_job_run_triggered_by");
        });

        modelBuilder.Entity<JobRunLog>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_run_log");
            entity.ToTable("job_run_log");
            entity.HasIndex(e => e.JobRunId, "ix_job_run_log_job_run_id");
            entity.HasIndex(e => e.Level, "ix_job_run_log_level");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Context).HasColumnName("context");
            entity.Property(e => e.JobRunId).HasColumnName("job_run_id");
            entity.Property(e => e.Level).HasMaxLength(13).HasColumnName("level");
            entity.Property(e => e.Message).HasMaxLength(1000).HasColumnName("message");
            entity.Property(e => e.StackTrace).HasColumnName("stack_trace");
            entity.Property(e => e.Timestamp).HasColumnType("timestamp with time zone").HasColumnName("timestamp");
            entity.HasOne(d => d.JobRun)
                .WithMany(p => p.JobRunLogs)
                .HasForeignKey(d => d.JobRunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_run_log_job_run_job_run_id");
        });

        modelBuilder.Entity<JobRunParameter>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_run_parameter");
            entity.ToTable("job_run_parameter");
            entity.HasIndex(e => e.JobRunId, "ix_job_run_parameter_job_run_id");
            entity.HasIndex(e => e.Key, "ix_job_run_parameter_key");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Description).HasMaxLength(100).HasColumnName("description");
            entity.Property(e => e.EncryptedValue).HasColumnName("encrypted_value");
            entity.Property(e => e.JobRunId).HasColumnName("job_run_id");
            entity.Property(e => e.Key).HasMaxLength(50).HasColumnName("key");
            entity.Property(e => e.Type).HasMaxLength(15).HasColumnName("type");
            entity.Property(e => e.Value).HasMaxLength(300).HasColumnName("value");
            entity.HasOne(d => d.JobRun)
                .WithMany(p => p.JobRunParameters)
                .HasForeignKey(d => d.JobRunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_run_parameter_job_run_job_run_id");
        });

        modelBuilder.Entity<JobRunResult>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_run_result");
            entity.ToTable("job_run_result");
            entity.HasIndex(e => e.JobRunId, "ix_job_run_result_job_run_id");
            entity.HasIndex(e => e.Key, "ix_job_run_result_key");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.JobRunId).HasColumnName("job_run_id");
            entity.Property(e => e.Key).HasMaxLength(50).HasColumnName("key");
            entity.Property(e => e.Type).HasMaxLength(15).HasColumnName("type");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.HasOne(d => d.JobRun)
                .WithMany(p => p.JobRunResults)
                .HasForeignKey(d => d.JobRunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_run_result_job_run_job_run_id");
        });

        modelBuilder.Entity<JobSchedule>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_schedule");
            entity.ToTable("job_schedule");
            entity.HasIndex(e => e.JobDefinitionId, "ix_job_schedule_job_definition_id");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.DayFlags).HasMaxLength(51).HasColumnName("day_flags");
            entity.Property(e => e.Description).HasMaxLength(100).HasColumnName("description");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.EndTime).HasMaxLength(8).HasColumnName("end_time");
            entity.Property(e => e.IntervalMinutes).HasColumnName("interval_minutes");
            entity.Property(e => e.JobDefinitionId).HasColumnName("job_definition_id");
            entity.Property(e => e.MonthFlags).HasMaxLength(108).HasColumnName("month_flags");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.Property(e => e.StartTime).HasMaxLength(8).HasColumnName("start_time");
            entity.Property(e => e.Times).HasColumnType("character varying(8)[]").HasColumnName("times");
            entity.Property(e => e.Type).HasMaxLength(8).HasColumnName("type");
            entity.HasOne(d => d.JobDefinition)
                .WithMany(p => p.JobSchedules)
                .HasForeignKey(d => d.JobDefinitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_schedule_job_definition_job_definition_id");
        });

        modelBuilder.Entity<JobScheduleParameter>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_schedule_parameter");
            entity.ToTable("job_schedule_parameter");
            entity.HasIndex(e => e.JobScheduleId, "ix_job_schedule_parameter_job_schedule_id");
            entity.HasIndex(e => e.Key, "ix_job_schedule_parameter_key");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Description).HasMaxLength(100).HasColumnName("description");
            entity.Property(e => e.Enabled).HasDefaultValue(true).HasColumnName("enabled");
            entity.Property(e => e.JobScheduleId).HasColumnName("job_schedule_id");
            entity.Property(e => e.Key).HasMaxLength(50).HasColumnName("key");
            entity.Property(e => e.Type).HasMaxLength(15).HasColumnName("type");
            entity.Property(e => e.Value).HasMaxLength(300).HasColumnName("value");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.HasOne(d => d.JobSchedule)
                .WithMany(p => p.JobScheduleParameters)
                .HasForeignKey(d => d.JobScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_schedule_parameter_job_schedule_job_schedule_id");
        });

        modelBuilder.Entity<JobTrigger>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_trigger");
            entity.ToTable("job_trigger");
            entity.HasIndex(e => e.JobDefinitionId, "ix_job_trigger_job_definition_id");
            entity.HasIndex(e => e.TriggerJobResultKey, "ix_job_trigger_trigger_job_result_key");
            entity.HasIndex(e => e.TriggersJobDefinitionId, "ix_job_trigger_triggers_job_definition_id");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Description).HasMaxLength(100).HasColumnName("description");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.JobDefinitionId).HasColumnName("job_definition_id");
            entity.Property(e => e.TriggerComparator).HasMaxLength(20).HasColumnName("trigger_comparator");
            entity.Property(e => e.TriggerJobResultKey).HasMaxLength(25).HasColumnName("trigger_job_result_key");
            entity.Property(e => e.TriggerJobResultValue).HasMaxLength(50).HasColumnName("trigger_job_result_value");
            entity.Property(e => e.TriggersJobDefinitionId).HasColumnName("triggers_job_definition_id");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.HasOne(d => d.JobDefinition)
                .WithMany(p => p.JobTriggerJobDefinitions)
                .HasForeignKey(d => d.JobDefinitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_trigger_job_definition_job_definition_id");

            entity.HasOne(d => d.TriggersJobDefinition)
                .WithMany(p => p.JobTriggerTriggersJobDefinitions)
                .HasForeignKey(d => d.TriggersJobDefinitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_trigger_triggers_job_definition");
        });

        modelBuilder.Entity<JobTriggerParameter>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_trigger_parameter");
            entity.ToTable("job_trigger_parameter");
            entity.HasIndex(e => e.JobTriggerId, "ix_job_trigger_parameter_job_trigger_id");
            entity.HasIndex(e => e.Key, "ix_job_trigger_parameter_key");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Description).HasMaxLength(100).HasColumnName("description");
            entity.Property(e => e.Enabled).HasDefaultValue(true).HasColumnName("enabled");
            entity.Property(e => e.JobTriggerId).HasColumnName("job_trigger_id");
            entity.Property(e => e.Key).HasMaxLength(50).HasColumnName("key");
            entity.Property(e => e.Type).HasMaxLength(15).HasColumnName("type");
            entity.Property(e => e.Value).HasMaxLength(300).HasColumnName("value");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.HasOne(d => d.JobTrigger)
                .WithMany(p => p.JobTriggerParameters)
                .HasForeignKey(d => d.JobTriggerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_trigger_parameter_job_trigger_job_trigger_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added) {
                if (entry.Entity is JobDefinition d) {
                    if (d.CreatedTimestamp == default)
                        d.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobFileUpload f) {
                    if (f.CreatedTimestamp == default)
                        f.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobParallelRestriction r) {
                    if (r.CreatedTimestamp == default)
                        r.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobParameter p) {
                    if (p.CreatedTimestamp == default)
                        p.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobSchedule s) {
                    if (s.CreatedTimestamp == default)
                        s.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobScheduleParameter sp) {
                    if (sp.CreatedTimestamp == default)
                        sp.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobTrigger t) {
                    if (t.CreatedTimestamp == default)
                        t.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobTriggerParameter tp) {
                    if (tp.CreatedTimestamp == default)
                        tp.CreatedTimestamp = now;
                }
            }
            else if (entry.State == EntityState.Modified) {
                if (entry.Entity is JobDefinition d)
                    d.UpdatedTimestamp = now;
                else if (entry.Entity is JobFileUpload f)
                    f.UpdatedTimestamp = now;
                else if (entry.Entity is JobParallelRestriction r)
                    r.UpdatedTimestamp = now;
                else if (entry.Entity is JobParameter p)
                    p.UpdatedTimestamp = now;
                else if (entry.Entity is JobRun run)
                    run.UpdatedTimestamp = now;
                else if (entry.Entity is JobSchedule s)
                    s.UpdatedTimestamp = now;
                else if (entry.Entity is JobScheduleParameter sp)
                    sp.UpdatedTimestamp = now;
                else if (entry.Entity is JobTrigger t)
                    t.UpdatedTimestamp = now;
                else if (entry.Entity is JobTriggerParameter tp)
                    tp.UpdatedTimestamp = now;
            }
        }

        return base.SaveChanges();
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}