namespace Lyo.StateMachine.Postgres.Database;

/// <summary>Current snapshot of one state machine instance. Primary key is (<see cref="MachineName" />, <see cref="InstanceKey" />).</summary>
public sealed class StateMachineInstanceEntity
{
    public string MachineName { get; set; } = string.Empty;

    public string InstanceKey { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    /// <summary>Snapshot version, used as the optimistic concurrency token.</summary>
    public long Version { get; set; }

    public DateTime EnteredTimestamp { get; set; }

    public DateTime UpdatedTimestamp { get; set; }

    /// <summary>Earliest timer due time, indexed for due-instance polling. Null when the state has no timer.</summary>
    public DateTime? DueTimestamp { get; set; }

    /// <summary>Instance data as JSON (<c>jsonb</c>).</summary>
    public string? DataJson { get; set; }
}