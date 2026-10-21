using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>What is happening during a transition. Passed to entry, exit, transition, and internal hooks.</summary>
/// <typeparam name="TState">State type.</typeparam>
/// <typeparam name="TTrigger">Trigger type.</typeparam>
/// <typeparam name="TContext">Host object the machine drives (character, job, worker).</typeparam>
/// <param name="Context">The host object passed to <c>FireAsync</c>.</param>
/// <param name="Source">State the instance is leaving (the innermost current state).</param>
/// <param name="Destination">State the instance is entering, after any initial-substate drill-down. Equal to <paramref name="Source" /> for re-entry and internal actions.</param>
/// <param name="Trigger">Trigger that caused the transition.</param>
/// <param name="Payload">Payload for <see cref="Trigger{TTrigger,TPayload}" /> triggers, otherwise null. Prefer <c>OnEntryFrom</c> for typed access.</param>
/// <param name="Kind">Transition, re-entry, or internal.</param>
public sealed record TransitionInfo<TState, TTrigger, TContext>(TContext Context, TState Source, TState Destination, TTrigger Trigger, object? Payload, TransitionKind Kind)
    where TState : notnull where TTrigger : notnull;