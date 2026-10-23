using System.Diagnostics;

namespace Lyo.StateMachine.Postgres.Database;

/// <summary>One history row written in the same transaction as the instance update.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class StateMachineTransitionEntity
{
    public Guid Id { get; set; }

    public string MachineName { get; set; } = string.Empty;

    public string InstanceKey { get; set; } = string.Empty;

    public string FromState { get; set; } = string.Empty;

    public string ToState { get; set; } = string.Empty;

    public string Trigger { get; set; } = string.Empty;

    /// <summary>Instance version produced by this transition.</summary>
    public long Version { get; set; }

    public DateTime OccurredTimestamp { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}