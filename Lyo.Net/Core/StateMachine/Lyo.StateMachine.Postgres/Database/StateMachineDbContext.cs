using Microsoft.EntityFrameworkCore;

namespace Lyo.StateMachine.Postgres.Database;

/// <summary>EF Core context for state machine instances, transition history, and definition documents in PostgreSQL.</summary>
public sealed class StateMachineDbContext : DbContext
{
    /// <summary>Mints a context with the supplied options.</summary>
    public StateMachineDbContext(DbContextOptions<StateMachineDbContext> options)
        : base(options) { }

    /// <summary>Current snapshot per instance.</summary>
    public DbSet<StateMachineInstanceEntity> Instances { get; set; } = null!;

    /// <summary>Transition history.</summary>
    public DbSet<StateMachineTransitionEntity> Transitions { get; set; } = null!;

    /// <summary>Saved state machine document revisions.</summary>
    public DbSet<StateMachineDefinitionEntity> Definitions { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PostgresStateMachineOptions.Schema);
        modelBuilder.Entity<StateMachineInstanceEntity>(entity => {
            entity.ToTable("instance");
            entity.HasKey(e => new { e.MachineName, e.InstanceKey }).HasName("pk_state_machine_instance");
            entity.Property(e => e.MachineName).HasMaxLength(200).HasColumnName("machine_name");
            entity.Property(e => e.InstanceKey).HasMaxLength(200).HasColumnName("instance_key");
            entity.Property(e => e.State).HasMaxLength(200).IsRequired().HasColumnName("state");
            entity.Property(e => e.Version).IsConcurrencyToken().HasColumnName("version");
            entity.Property(e => e.EnteredTimestamp).HasColumnType("timestamp with time zone").HasColumnName("entered_timestamp");
            entity.Property(e => e.UpdatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("updated_timestamp");
            entity.Property(e => e.DueTimestamp).HasColumnType("timestamp with time zone").HasColumnName("due_at");
            entity.Property(e => e.DataJson).HasColumnType("jsonb").HasColumnName("data_json");
            entity.HasIndex(e => new { e.MachineName, e.State }).HasDatabaseName("ix_state_machine_instance_machine_state");
            entity.HasIndex(e => new { e.MachineName, e.DueTimestamp }).HasDatabaseName("ix_state_machine_instance_machine_due");
        });

        modelBuilder.Entity<StateMachineDefinitionEntity>(entity => {
            entity.ToTable("definition");
            entity.HasKey(e => new { e.Name, e.Revision }).HasName("pk_state_machine_definition");
            entity.Property(e => e.Name).HasMaxLength(200).HasColumnName("name");
            entity.Property(e => e.Revision).HasColumnName("revision");
            entity.Property(e => e.Document).HasColumnType("jsonb").IsRequired().HasColumnName("document");
            entity.Property(e => e.CreatedTimestamp).HasColumnType("timestamp with time zone").HasColumnName("created_timestamp");
        });

        modelBuilder.Entity<StateMachineTransitionEntity>(entity => {
            entity.ToTable("transition");
            entity.HasKey(e => e.Id).HasName("pk_state_machine_transition");
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(e => e.MachineName).HasMaxLength(200).IsRequired().HasColumnName("machine_name");
            entity.Property(e => e.InstanceKey).HasMaxLength(200).IsRequired().HasColumnName("instance_key");
            entity.Property(e => e.FromState).HasMaxLength(200).IsRequired().HasColumnName("from_state");
            entity.Property(e => e.ToState).HasMaxLength(200).IsRequired().HasColumnName("to_state");
            entity.Property(e => e.Trigger).HasMaxLength(200).IsRequired().HasColumnName("trigger");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.OccurredTimestamp).HasColumnType("timestamp with time zone").HasColumnName("occurred_timestamp");
            entity.HasIndex(e => new { e.MachineName, e.InstanceKey, e.Version }).IsUnique().HasDatabaseName("ux_state_machine_transition_instance_version");
        });
    }
}
