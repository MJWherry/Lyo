using Lyo.Job.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Job.Postgres.Database;

public partial class JobContext : DbContext
{
    public virtual DbSet<JobBlackoutCalendar> JobBlackoutCalendars { get; set; }

    public virtual DbSet<JobBlackoutWindow> JobBlackoutWindows { get; set; }

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

    public virtual DbSet<JobWorkerInstance> JobWorkerInstances { get; set; }

    public virtual DbSet<JobWorkflow> JobWorkflows { get; set; }

    public virtual DbSet<JobWorkflowRun> JobWorkflowRuns { get; set; }

    public virtual DbSet<JobWorkflowRunStep> JobWorkflowRunSteps { get; set; }

    public virtual DbSet<JobWorkflowStep> JobWorkflowSteps { get; set; }

    public JobContext(DbContextOptions<JobContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("job");
        modelBuilder.Entity<JobBlackoutCalendar>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_blackout_calendar");
            entity.ToTable("job_blackout_calendar");
            entity.HasIndex(e => e.Name, "ix_job_blackout_calendar_name");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        });

        modelBuilder.Entity<JobBlackoutWindow>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_blackout_window");
            entity.ToTable("job_blackout_window");
            entity.HasIndex(e => e.JobBlackoutCalendarId, "ix_job_blackout_window_job_blackout_calendar_id");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.JobBlackoutCalendarId).HasColumnName("job_blackout_calendar_id");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.DayFlags).HasMaxLength(51).HasColumnName("day_flags");
            entity.Property(e => e.StartDateUtc).HasColumnType("timestamp with time zone").HasColumnName("start_date_utc");
            entity.Property(e => e.EndDateUtc).HasColumnType("timestamp with time zone").HasColumnName("end_date_utc");
            entity.Property(e => e.StartTime).HasMaxLength(8).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasMaxLength(8).HasColumnName("end_time");
            entity.Property(e => e.Policy).HasMaxLength(10).HasDefaultValue(nameof(JobBlackoutPolicy.Skip)).HasColumnName("policy");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.HasOne(d => d.JobBlackoutCalendar)
                .WithMany(p => p.JobBlackoutWindows)
                .HasForeignKey(d => d.JobBlackoutCalendarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_blackout_window_job_blackout_calendar_job_blackout_calendar_id");
        });

        modelBuilder.Entity<JobDefinition>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_definition");
            entity.ToTable("job_definition");
            entity.HasIndex(e => e.Name, "ix_job_definition_name");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Description).HasMaxLength(3000).HasColumnName("description");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Type).HasMaxLength(25).HasColumnName("type");
            entity.Property(e => e.WorkerType).HasMaxLength(7).HasColumnName("worker_type");
            entity.Property(e => e.MaxRetryCount).HasDefaultValue(0).HasColumnName("max_retry_count");
            entity.Property(e => e.RetryBackoffSeconds).HasDefaultValue(0).HasColumnName("retry_backoff_seconds");
            entity.Property(e => e.RetryBackoffType).HasMaxLength(12).HasDefaultValue(nameof(JobRetryBackoffType.Linear)).HasColumnName("retry_backoff_type");
            entity.Property(e => e.Priority).HasDefaultValue(0).HasColumnName("priority");
            entity.Property(e => e.RetentionDays).HasDefaultValue(0).HasColumnName("retention_days");
            entity.Property(e => e.TimeoutMinutes).HasDefaultValue(0).HasColumnName("timeout_minutes");
            entity.Property(e => e.MaxConcurrentRuns).HasDefaultValue(0).HasColumnName("max_concurrent_runs");
            entity.Property(e => e.CircuitBreakerThreshold).HasDefaultValue(0).HasColumnName("circuit_breaker_threshold");
            entity.Property(e => e.CircuitBreakerResetMinutes).HasDefaultValue(0).HasColumnName("circuit_breaker_reset_minutes");
            entity.Property(e => e.CircuitBreakerTrippedAt).HasColumnType("timestamp with time zone").HasColumnName("circuit_breaker_tripped_at");
            entity.Property(e => e.MaxRunsPerHour).HasDefaultValue(0).HasColumnName("max_runs_per_hour");
            entity.Property(e => e.ExpectedDurationMinutes).HasDefaultValue(0).HasColumnName("expected_duration_minutes");
            entity.Property(e => e.MustStartByMinutes).HasDefaultValue(0).HasColumnName("must_start_by_minutes");
            entity.Property(e => e.AlertOnFailure).HasDefaultValue(false).HasColumnName("alert_on_failure");
            entity.Property(e => e.AlertAfterConsecutiveFailures).HasDefaultValue(0).HasColumnName("alert_after_consecutive_failures");
            entity.Property(e => e.AlertWebhookUrl).HasMaxLength(500).HasColumnName("alert_webhook_url");
            entity.Property(e => e.DefinitionVersion).HasDefaultValue(1).HasColumnName("definition_version");
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
            entity.Property(e => e.Description).HasMaxLength(3000).HasColumnName("description");
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
            entity.Property(e => e.Description).HasMaxLength(3000).HasColumnName("description");
            entity.Property(e => e.EncryptedValue).HasColumnName("encrypted_value");
            entity.Property(e => e.JobDefinitionId).HasColumnName("job_definition_id");
            entity.Property(e => e.Key).HasMaxLength(50).HasColumnName("key");
            entity.Property(e => e.Required).HasDefaultValue(true).HasColumnName("required");
            entity.Property(e => e.Type).HasMaxLength(15).HasColumnName("type");
            entity.Property(e => e.Value).HasMaxLength(3000).HasColumnName("value");
            entity.Property(e => e.ValidationRegex).HasMaxLength(500).HasColumnName("validation_regex");
            entity.Property(e => e.MinLength).HasColumnName("min_length");
            entity.Property(e => e.MaxLength).HasColumnName("max_length");
            entity.Property(e => e.AllowedValues).HasMaxLength(1000).HasColumnName("allowed_values");
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
            entity.Property(e => e.Result).HasConversion(v => v == null ? null : v.ToString(), v => ToJobRunResult(v)).HasMaxLength(20).HasColumnName("result");
            entity.Property(e => e.StartedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("started_timestamp");
            entity.Property(e => e.State).HasConversion(v => v.ToString(), v => ToJobState(v)).HasMaxLength(12).HasColumnName("state");
            entity.Property(e => e.TriggeredByJobRunId).HasColumnName("triggered_by_job_run_id");
            entity.Property(e => e.ScheduledSlotUtc).HasColumnType("timestamp with time zone").HasColumnName("scheduled_slot_utc");
            entity.Property(e => e.RetryAttempt).HasDefaultValue(0).HasColumnName("retry_attempt");
            entity.Property(e => e.LastHeartbeatUtc).HasColumnType("timestamp with time zone").HasColumnName("last_heartbeat_utc");
            entity.Property(e => e.Priority).HasDefaultValue(0).HasColumnName("priority");
            entity.Property(e => e.ProgressPercent).HasColumnName("progress_percent");
            entity.Property(e => e.ProgressMessage).HasMaxLength(500).HasColumnName("progress_message");
            entity.Property(e => e.IdempotencyKey).HasMaxLength(128).HasColumnName("idempotency_key");
            entity.Property(e => e.DryRun).HasDefaultValue(false).HasColumnName("dry_run");
            entity.Property(e => e.SlaBreached).HasDefaultValue(false).HasColumnName("sla_breached");
            entity.Property(e => e.TraceId).HasMaxLength(64).HasColumnName("trace_id");
            entity.Property(e => e.ParentJobRunId).HasColumnName("parent_job_run_id");
            entity.Property(e => e.BatchIndex).HasColumnName("batch_index");
            entity.Property(e => e.BatchTotal).HasColumnName("batch_total");
            entity.Property(e => e.DefinitionAuditVersion).HasColumnName("definition_audit_version");
            entity.HasIndex(e => new { e.JobDefinitionId, e.IdempotencyKey })
                .HasFilter("idempotency_key IS NOT NULL")
                .IsUnique()
                .HasDatabaseName("ix_job_run_idempotency_key_unique");
            entity.HasIndex(e => e.ParentJobRunId, "ix_job_run_parent_job_run_id");
            entity.HasIndex(e => new { e.JobScheduleId, e.ScheduledSlotUtc })
                .HasFilter("job_schedule_id IS NOT NULL AND scheduled_slot_utc IS NOT NULL")
                .IsUnique()
                .HasDatabaseName("ix_job_run_schedule_slot_unique");

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
            entity.HasOne(d => d.ParentJobRun)
                .WithMany(p => p.InverseParentJobRun)
                .HasForeignKey(d => d.ParentJobRunId)
                .HasConstraintName("fk_job_run_parent");
        });

        modelBuilder.Entity<JobRunLog>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_run_log");
            entity.ToTable("job_run_log");
            entity.HasIndex(e => e.JobRunId, "ix_job_run_log_job_run_id");
            entity.HasIndex(e => e.Level, "ix_job_run_log_level");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Context).HasMaxLength(16_384).HasColumnName("context");
            entity.Property(e => e.JobRunId).HasColumnName("job_run_id");
            entity.Property(e => e.Level).HasMaxLength(13).HasColumnName("level");
            entity.Property(e => e.Message).HasMaxLength(1000).HasColumnName("message");
            entity.Property(e => e.StackTrace).HasMaxLength(16384).HasColumnName("stack_trace");
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
            entity.Property(e => e.Description).HasMaxLength(3000).HasColumnName("description");
            entity.Property(e => e.EncryptedValue).HasColumnName("encrypted_value");
            entity.Property(e => e.JobRunId).HasColumnName("job_run_id");
            entity.Property(e => e.Key).HasMaxLength(50).HasColumnName("key");
            entity.Property(e => e.Type).HasMaxLength(15).HasColumnName("type");
            entity.Property(e => e.Value).HasMaxLength(3000).HasColumnName("value");
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
            entity.Property(e => e.Value).HasMaxLength(16_384).HasColumnName("value");
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
            entity.HasIndex(e => e.JobBlackoutCalendarId, "ix_job_schedule_job_blackout_calendar_id");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.DayFlags).HasMaxLength(51).HasColumnName("day_flags");
            entity.Property(e => e.Description).HasMaxLength(3000).HasColumnName("description");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.EndTime).HasMaxLength(8).HasColumnName("end_time");
            entity.Property(e => e.IntervalMinutes).HasColumnName("interval_minutes");
            entity.Property(e => e.CronExpression).HasMaxLength(120).HasColumnName("cron_expression");
            entity.Property(e => e.MisfirePolicy).HasMaxLength(12).HasDefaultValue(nameof(JobMisfirePolicy.Skip)).HasColumnName("misfire_policy");
            entity.Property(e => e.StartDateUtc).HasColumnType("timestamp with time zone").HasColumnName("start_date_utc");
            entity.Property(e => e.EndDateUtc).HasColumnType("timestamp with time zone").HasColumnName("end_date_utc");
            entity.Property(e => e.TimeZoneId).HasMaxLength(64).HasColumnName("time_zone_id");
            entity.Property(e => e.JobBlackoutCalendarId).HasColumnName("job_blackout_calendar_id");
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
            entity.HasOne(d => d.JobBlackoutCalendar)
                .WithMany(p => p.JobSchedules)
                .HasForeignKey(d => d.JobBlackoutCalendarId)
                .HasConstraintName("fk_job_schedule_job_blackout_calendar_job_blackout_calendar_id");
        });

        modelBuilder.Entity<JobScheduleParameter>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_schedule_parameter");
            entity.ToTable("job_schedule_parameter");
            entity.HasIndex(e => e.JobScheduleId, "ix_job_schedule_parameter_job_schedule_id");
            entity.HasIndex(e => e.Key, "ix_job_schedule_parameter_key");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Description).HasMaxLength(3000).HasColumnName("description");
            entity.Property(e => e.Enabled).HasDefaultValue(true).HasColumnName("enabled");
            entity.Property(e => e.JobScheduleId).HasColumnName("job_schedule_id");
            entity.Property(e => e.Key).HasMaxLength(50).HasColumnName("key");
            entity.Property(e => e.Type).HasMaxLength(15).HasColumnName("type");
            entity.Property(e => e.Value).HasMaxLength(3000).HasColumnName("value");
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
            entity.Property(e => e.Description).HasMaxLength(3000).HasColumnName("description");
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
            entity.Property(e => e.Description).HasMaxLength(3000).HasColumnName("description");
            entity.Property(e => e.Enabled).HasDefaultValue(true).HasColumnName("enabled");
            entity.Property(e => e.JobTriggerId).HasColumnName("job_trigger_id");
            entity.Property(e => e.Key).HasMaxLength(50).HasColumnName("key");
            entity.Property(e => e.Type).HasMaxLength(15).HasColumnName("type");
            entity.Property(e => e.Value).HasMaxLength(3000).HasColumnName("value");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.HasOne(d => d.JobTrigger)
                .WithMany(p => p.JobTriggerParameters)
                .HasForeignKey(d => d.JobTriggerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_trigger_parameter_job_trigger_job_trigger_id");
        });

        modelBuilder.Entity<JobWorkerInstance>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_worker_instance");
            entity.ToTable("job_worker_instance");
            entity.HasIndex(e => e.WorkerType, "ix_job_worker_instance_worker_type");
            entity.HasIndex(e => e.LastHeartbeatUtc, "ix_job_worker_instance_last_heartbeat_utc");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.WorkerType).HasMaxLength(50).HasColumnName("worker_type");
            entity.Property(e => e.MachineName).HasMaxLength(100).HasColumnName("machine_name");
            entity.Property(e => e.ProcessId).HasColumnName("process_id");
            entity.Property(e => e.State).HasMaxLength(10).HasColumnName("state");
            entity.Property(e => e.InFlightCount).HasDefaultValue(0).HasColumnName("in_flight_count");
            entity.Property(e => e.StartedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("started_timestamp");
            entity.Property(e => e.LastHeartbeatUtc).HasColumnType("timestamp with time zone").HasColumnName("last_heartbeat_utc");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        });

        modelBuilder.Entity<JobWorkflow>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_workflow");
            entity.ToTable("job_workflow");
            entity.HasIndex(e => e.Name, "ix_job_workflow_name");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
        });

        modelBuilder.Entity<JobWorkflowStep>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_workflow_step");
            entity.ToTable("job_workflow_step");
            entity.HasIndex(e => e.JobWorkflowId, "ix_job_workflow_step_job_workflow_id");
            entity.HasIndex(e => e.JobDefinitionId, "ix_job_workflow_step_job_definition_id");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.JobWorkflowId).HasColumnName("job_workflow_id");
            entity.Property(e => e.JobDefinitionId).HasColumnName("job_definition_id");
            entity.Property(e => e.StepName).HasMaxLength(100).HasColumnName("step_name");
            entity.Property(e => e.StepOrder).HasColumnName("step_order");
            entity.Property(e => e.DependsOnStepIds).HasColumnName("depends_on_step_ids");
            entity.Property(e => e.FailurePolicy).HasMaxLength(20).HasDefaultValue(nameof(JobWorkflowFailurePolicy.Stop)).HasColumnName("failure_policy");
            entity.Property(e => e.ParametersJson).HasColumnName("parameters_json");
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.HasOne(d => d.JobWorkflow)
                .WithMany(p => p.JobWorkflowSteps)
                .HasForeignKey(d => d.JobWorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_workflow_step_job_workflow_job_workflow_id");
            entity.HasOne(d => d.JobDefinition)
                .WithMany(p => p.JobWorkflowSteps)
                .HasForeignKey(d => d.JobDefinitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_workflow_step_job_definition_job_definition_id");
        });

        modelBuilder.Entity<JobWorkflowRun>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_workflow_run");
            entity.ToTable("job_workflow_run");
            entity.HasIndex(e => e.JobWorkflowId, "ix_job_workflow_run_job_workflow_id");
            entity.HasIndex(e => e.State, "ix_job_workflow_run_state");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.JobWorkflowId).HasColumnName("job_workflow_id");
            entity.Property(e => e.State).HasConversion(v => v.ToString(), v => ToJobWorkflowRunState(v)).HasMaxLength(20).HasColumnName("state");
            entity.Property(e => e.StartedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("started_timestamp");
            entity.Property(e => e.FinishedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("finished_timestamp");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.HasOne(d => d.JobWorkflow)
                .WithMany(p => p.JobWorkflowRuns)
                .HasForeignKey(d => d.JobWorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_workflow_run_job_workflow_job_workflow_id");
        });

        modelBuilder.Entity<JobWorkflowRunStep>(entity => {
            entity.HasKey(e => e.Id).HasName("pk_job_workflow_run_step");
            entity.ToTable("job_workflow_run_step");
            entity.HasIndex(e => e.JobWorkflowRunId, "ix_job_workflow_run_step_job_workflow_run_id");
            entity.HasIndex(e => e.JobWorkflowStepId, "ix_job_workflow_run_step_job_workflow_step_id");
            entity.HasIndex(e => e.JobRunId, "ix_job_workflow_run_step_job_run_id");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").HasColumnName("id");
            entity.Property(e => e.JobWorkflowRunId).HasColumnName("job_workflow_run_id");
            entity.Property(e => e.JobWorkflowStepId).HasColumnName("job_workflow_step_id");
            entity.Property(e => e.JobRunId).HasColumnName("job_run_id");
            entity.Property(e => e.State).HasConversion(v => v.ToString(), v => ToJobWorkflowStepState(v)).HasMaxLength(20).HasColumnName("state");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.HasOne(d => d.JobWorkflowRun)
                .WithMany(p => p.JobWorkflowRunSteps)
                .HasForeignKey(d => d.JobWorkflowRunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_workflow_run_step_job_workflow_run_job_workflow_run_id");
            entity.HasOne(d => d.JobWorkflowStep)
                .WithMany(p => p.JobWorkflowRunSteps)
                .HasForeignKey(d => d.JobWorkflowStepId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_workflow_run_step_job_workflow_step_job_workflow_step_id");
            entity.HasOne(d => d.JobRun)
                .WithMany(p => p.JobWorkflowRunSteps)
                .HasForeignKey(d => d.JobRunId)
                .HasConstraintName("fk_job_workflow_run_step_job_run_job_run_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    private static Models.Enums.JobRunResult? ToJobRunResult(string? v) => v == null ? null : (Models.Enums.JobRunResult?)Enum.Parse(typeof(Models.Enums.JobRunResult), v, true);

    private static JobState ToJobState(string v) => (JobState)Enum.Parse(typeof(JobState), v, true);

    private static Models.Enums.JobWorkflowRunState ToJobWorkflowRunState(string v)
        => (Models.Enums.JobWorkflowRunState)Enum.Parse(typeof(Models.Enums.JobWorkflowRunState), v, true);

    private static Models.Enums.JobWorkflowStepState ToJobWorkflowStepState(string v)
        => (Models.Enums.JobWorkflowStepState)Enum.Parse(typeof(Models.Enums.JobWorkflowStepState), v, true);

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()) {
            if (entry.State == EntityState.Added) {
                if (entry.Entity is JobBlackoutCalendar c) {
                    if (c.CreatedTimestamp == default)
                        c.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobBlackoutWindow cw) {
                    if (cw.CreatedTimestamp == default)
                        cw.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobDefinition d) {
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
                else if (entry.Entity is JobWorkerInstance w) {
                    if (w.CreatedTimestamp == default)
                        w.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobWorkflow wf) {
                    if (wf.CreatedTimestamp == default)
                        wf.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobWorkflowStep wfs) {
                    if (wfs.CreatedTimestamp == default)
                        wfs.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobWorkflowRun wfr) {
                    if (wfr.CreatedTimestamp == default)
                        wfr.CreatedTimestamp = now;
                }
                else if (entry.Entity is JobWorkflowRunStep wfrs) {
                    if (wfrs.CreatedTimestamp == default)
                        wfrs.CreatedTimestamp = now;
                }
            }
            else if (entry.State == EntityState.Modified) {
                if (entry.Entity is JobBlackoutCalendar c)
                    c.UpdatedTimestamp = now;
                else if (entry.Entity is JobBlackoutWindow cw)
                    cw.UpdatedTimestamp = now;
                else if (entry.Entity is JobDefinition d)
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
                else if (entry.Entity is JobWorkerInstance w)
                    w.UpdatedTimestamp = now;
                else if (entry.Entity is JobWorkflow wf)
                    wf.UpdatedTimestamp = now;
                else if (entry.Entity is JobWorkflowStep wfs)
                    wfs.UpdatedTimestamp = now;
                else if (entry.Entity is JobWorkflowRun wfr)
                    wfr.UpdatedTimestamp = now;
                else if (entry.Entity is JobWorkflowRunStep wfrs)
                    wfrs.UpdatedTimestamp = now;
            }
        }

        return base.SaveChanges();
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}