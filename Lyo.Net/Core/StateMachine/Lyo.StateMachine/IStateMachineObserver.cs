using System.Diagnostics;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>An accepted trigger, as reported to <see cref="IStateMachineObserver{TState,TTrigger}" />.</summary>
/// <param name="MachineName">Name given to the <see cref="StateMachineBuilder{TState,TTrigger,TContext}" />.</param>
/// <param name="Source">State the trigger was fired from.</param>
/// <param name="Destination">State after the trigger (equal to <paramref name="Source" /> for re-entry, internal, and ignored).</param>
/// <param name="Trigger">Trigger fired.</param>
/// <param name="Version">Snapshot version after the trigger.</param>
/// <param name="Kind">Transition, re-entry, internal, or ignored.</param>
/// <param name="OccurredAtUtc">Time from the definition clock.</param>
/// <param name="Duration">Time spent in guards and hooks for this fire.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StateTransitioned<TState, TTrigger>(
    string MachineName,
    TState Source,
    TState Destination,
    TTrigger Trigger,
    long Version,
    TransitionKind Kind,
    DateTime OccurredAtUtc,
    TimeSpan Duration)
    where TState : notnull where TTrigger : notnull;

/// <summary>A rejected trigger, as reported to <see cref="IStateMachineObserver{TState,TTrigger}" />.</summary>
/// <param name="MachineName">Machine name.</param>
/// <param name="State">State the trigger was fired from.</param>
/// <param name="Trigger">Trigger fired.</param>
/// <param name="Rejection">Why it was rejected.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record StateTransitionRejected<TState, TTrigger>(string MachineName, TState State, TTrigger Trigger, TransitionRejection Rejection)
    where TState : notnull where TTrigger : notnull;

/// <summary>Cross-cutting listener (logging, metrics, events) called after every fire, once all hooks have run.</summary>
/// <remarks>
/// Called synchronously on both the sync and async fire paths, so keep it fast; hand slow work to a queue. Register with <c>AddStateMachineObserver</c>,
/// <c>AddStateMachineMetrics</c>, <c>AddStateMachineLogging</c>, or <see cref="StateMachineBuilder{TState,TTrigger,TContext}.AddObserver" />. Exceptions propagate to the caller of
/// <c>Fire</c>.
/// </remarks>
public interface IStateMachineObserver<TState, TTrigger>
    where TState : notnull where TTrigger : notnull
{
    /// <summary>Called after a trigger is accepted (transition, re-entry, internal, or ignored).</summary>
    void OnTransitioned(StateTransitioned<TState, TTrigger> transition);

    /// <summary>Called after a trigger is rejected (no rule or every guard failed). Not called for concurrency conflicts, which happen in the store.</summary>
    void OnRejected(StateTransitionRejected<TState, TTrigger> rejection);
}