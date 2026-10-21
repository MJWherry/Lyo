using System.Text.Json;

namespace Lyo.StateMachine;

/// <summary>Behaviour of a <see cref="StateMachineInstance{TState,TTrigger,TContext}" />.</summary>
public sealed class StateMachineInstanceOptions
{
    /// <summary>
    /// How many times to reload and fire again after a concurrency conflict. Default 0 (return the conflict). Hooks run again on every retry, so only raise this when hooks are
    /// idempotent.
    /// </summary>
    public int MaxConflictRetries { get; set; }

    /// <summary>Serializer options for instance data. Defaults to <c>LyoJsonSerializerOptions.Create()</c> (camelCase, string enums, nulls omitted).</summary>
    public JsonSerializerOptions? Json { get; set; }

    /// <summary>Throws when a value is out of range.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="MaxConflictRetries" /> is negative.</exception>
    public void Validate()
    {
        if (MaxConflictRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxConflictRetries), MaxConflictRetries, "Must be zero or more.");
    }
}