namespace Lyo.StateMachine;

/// <summary>Structural problems found by <c>StateMachineDefinition.Analyze</c>.</summary>
/// <param name="UnreachableStates">States no sequence of transitions from the initial state can reach, in declaration order.</param>
/// <param name="DeadEndStates">States with no transition out (re-entry, internal, and ignore rules do not count) that are not marked <c>AsFinal</c> and have no initial substate.</param>
public sealed record StateMachineAnalysis<TState>(IReadOnlyList<TState> UnreachableStates, IReadOnlyList<TState> DeadEndStates)
    where TState : notnull
{
    /// <summary>True when there are no unreachable or dead-end states.</summary>
    public bool IsClean => UnreachableStates.Count == 0 && DeadEndStates.Count == 0;
}