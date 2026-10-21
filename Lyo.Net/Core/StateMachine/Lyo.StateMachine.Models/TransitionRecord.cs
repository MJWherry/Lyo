namespace Lyo.StateMachine.Models;

/// <summary>One history entry written by a state store alongside the new snapshot.</summary>
/// <typeparam name="TState">State type, usually an enum.</typeparam>
/// <param name="From">State before the transition.</param>
/// <param name="To">State after the transition.</param>
/// <param name="Trigger">Trigger name (its <c>ToString()</c>).</param>
/// <param name="Version">Snapshot version produced by this transition.</param>
/// <param name="OccurredAtUtc">When the transition happened, in UTC.</param>
public sealed record TransitionRecord<TState>(TState From, TState To, string Trigger, long Version, DateTime OccurredAtUtc)
    where TState : notnull;