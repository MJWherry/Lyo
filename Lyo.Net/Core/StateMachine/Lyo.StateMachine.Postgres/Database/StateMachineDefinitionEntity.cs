namespace Lyo.StateMachine.Postgres.Database;

/// <summary>One immutable revision of a state machine document. Primary key is (<see cref="Name" />, <see cref="Revision" />).</summary>
public sealed class StateMachineDefinitionEntity
{
    public string Name { get; set; } = string.Empty;

    public int Revision { get; set; }

    /// <summary>The <c>StateMachineDocument</c> as JSON (<c>jsonb</c>).</summary>
    public string Document { get; set; } = string.Empty;

    public DateTime CreatedTimestamp { get; set; }
}
