using Lyo.Postgres;

namespace Lyo.StateMachine.Postgres;

/// <summary>Settings for PostgreSQL state machine storage.</summary>
public sealed class PostgresStateMachineOptions : PostgresOptionsBase
{
    /// <summary>Name of the configuration section.</summary>
    public const string SectionName = "PostgresStateMachine";

    /// <summary>PostgreSQL schema that holds state machine tables.</summary>
    public const string Schema = "state_machine";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}
