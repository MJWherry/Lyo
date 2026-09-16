using Lyo.Exceptions;
using Lyo.StateMachine.Internal;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>What a definition does with a trigger that has no rule in the current state or its ancestors.</summary>
public enum UnhandledTriggerPolicy
{
    /// <summary>Return <see cref="TransitionRejection.NoTransition" /> (default).</summary>
    Reject,

    /// <summary>Throw <see cref="UnhandledTriggerException" />. Guard failures are still returned as <see cref="TransitionRejection.GuardFailed" />.</summary>
    Throw
}

/// <summary>Fluent builder for an immutable <see cref="StateMachineDefinition{TState,TTrigger,TContext}" />.</summary>
/// <remarks>
/// Build once at startup (or through <c>AddStateMachine</c>) and share the definition across threads and entities. States are listed in the order they are first
/// mentioned, which is also the order used by <c>ToMermaid</c>/<c>ToDot</c>. <see cref="Build" /> throws <see cref="StateMachineConfigurationException" /> for
/// substate cycles, an initial substate that is not a direct child, ambiguous rules (two rules for one trigger where either is unguarded), one trigger used with
/// different payload types, and timers whose trigger is not permitted or takes a payload. With <see cref="Strict" /> it also throws for unreachable and dead-end
/// states.
/// </remarks>
/// <example>
/// <code>
/// var builder = new StateMachineBuilder&lt;JobState, JobTrigger, JobContext&gt;("job", JobState.Queued);
/// builder.Configure(JobState.Queued).Permit(JobTrigger.Start, JobState.Running);
/// builder.Configure(JobState.Paused).SubstateOf(JobState.Running);
/// var definition = builder.Build();
/// </code>
/// </example>
/// <typeparam name="TState">State type, usually an enum.</typeparam>
/// <typeparam name="TTrigger">Trigger type, usually an enum.</typeparam>
/// <typeparam name="TContext">Host object passed to guards and hooks (character, job row, worker).</typeparam>
public sealed class StateMachineBuilder<TState, TTrigger, TContext>
    where TState : notnull where TTrigger : notnull
{
    private readonly Dictionary<TState, StateNode<TState, TTrigger, TContext>> _nodes = new();
    private readonly List<IStateMachineObserver<TState, TTrigger>> _observers = [];
    private readonly List<TState> _order = [];
    private readonly List<Hook<TState, TTrigger, TContext>> _transitionHooks = [];
    private Func<DateTime> _clock = () => DateTime.UtcNow;
    private bool _guardReentrancy;
    private bool _strict;
    private Action<TContext, TState, TTrigger>? _unhandledCallback;
    private UnhandledTriggerPolicy _unhandledPolicy = UnhandledTriggerPolicy.Reject;

    /// <summary>Starts a machine definition.</summary>
    /// <param name="name">Stable machine name. Stores key saved instances by it, so do not rename a machine that has persisted data.</param>
    /// <param name="initialState">State new instances start in.</param>
    public StateMachineBuilder(string name, TState initialState)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNull(initialState);
        Name = name;
        InitialState = initialState;
        GetNode(initialState);
    }

    /// <summary>Machine name.</summary>
    public string Name { get; }

    /// <summary>State new instances start in.</summary>
    public TState InitialState { get; }

    /// <summary>Returns the configuration for <paramref name="state" />, creating it on first use. Calling it again for the same state adds to it.</summary>
    public StateConfiguration<TState, TTrigger, TContext> Configure(TState state)
    {
        ArgumentHelpers.ThrowIfNull(state);
        return new(this, GetNode(state));
    }

    /// <summary>Adds a hook that runs on every transition, after exit hooks and before entry hooks.</summary>
    public StateMachineBuilder<TState, TTrigger, TContext> OnTransition(Action<TransitionInfo<TState, TTrigger, TContext>> action)
    {
        ArgumentHelpers.ThrowIfNull(action);
        return AddTransitionHook(new(action, null));
    }

    /// <summary>Async form of <see cref="OnTransition" />. Machines with async hooks can only be fired with <c>FireAsync</c>.</summary>
    public StateMachineBuilder<TState, TTrigger, TContext> OnTransitionAsync(Func<TransitionInfo<TState, TTrigger, TContext>, CancellationToken, Task> action)
    {
        ArgumentHelpers.ThrowIfNull(action);
        return AddTransitionHook(new(null, action));
    }

    /// <summary>Adds an observer called after every accepted and rejected trigger.</summary>
    public StateMachineBuilder<TState, TTrigger, TContext> AddObserver(IStateMachineObserver<TState, TTrigger> observer)
    {
        ArgumentHelpers.ThrowIfNull(observer);
        _observers.Add(observer);
        return this;
    }

    /// <summary>Replaces the clock used for <see cref="StateSnapshot{TState}.EnteredAtUtc" /> and timers. It must return UTC. Useful for tests and game time.</summary>
    public StateMachineBuilder<TState, TTrigger, TContext> UseClock(Func<DateTime> utcNow)
    {
        ArgumentHelpers.ThrowIfNull(utcNow);
        _clock = utcNow;
        return this;
    }

    /// <summary>Sets what happens to triggers with no rule in the current state or its ancestors. The default is <see cref="UnhandledTriggerPolicy.Reject" />.</summary>
    public StateMachineBuilder<TState, TTrigger, TContext> OnUnhandledTrigger(UnhandledTriggerPolicy policy)
    {
        _unhandledPolicy = policy;
        _unhandledCallback = null;
        return this;
    }

    /// <summary>Calls <paramref name="callback" /> (context, state, trigger) for unhandled triggers, then returns <see cref="TransitionRejection.NoTransition" />.</summary>
    public StateMachineBuilder<TState, TTrigger, TContext> OnUnhandledTrigger(Action<TContext, TState, TTrigger> callback)
    {
        ArgumentHelpers.ThrowIfNull(callback);
        _unhandledPolicy = UnhandledTriggerPolicy.Reject;
        _unhandledCallback = callback;
        return this;
    }

    /// <summary>
    /// Throws <see cref="StateMachineReentrancyException" /> when a hook (directly or through awaited calls) fires this definition again for the same context
    /// object. Off by default because it costs an <see cref="AsyncLocal{T}" /> write per fire.
    /// </summary>
    public StateMachineBuilder<TState, TTrigger, TContext> GuardReentrancy()
    {
        _guardReentrancy = true;
        return this;
    }

    /// <summary>Makes <see cref="Build" /> throw when <c>Analyze</c> finds unreachable or dead-end states.</summary>
    public StateMachineBuilder<TState, TTrigger, TContext> Strict()
    {
        _strict = true;
        return this;
    }

    /// <summary>Validates the configuration and returns an immutable, precompiled definition. The builder can keep being used afterwards.</summary>
    /// <exception cref="StateMachineConfigurationException">The configuration is invalid.</exception>
    public StateMachineDefinition<TState, TTrigger, TContext> Build()
    {
        foreach (var state in _order)
            EnsureNoCycle(state);

        var payloadTypes = new Dictionary<TTrigger, Type?>();
        foreach (var node in _order.Select(s => _nodes[s])) {
            if (node.HasInitialSubstate && !IsDirectChild(node.InitialSubstate, node.State))
                throw new StateMachineConfigurationException($"Initial substate '{node.InitialSubstate}' of '{node.State}' is not a direct substate of it.");

            foreach (var group in node.Rules.GroupBy(r => r.Trigger)) {
                if (group.Count() > 1 && group.Any(r => r.Guard is null))
                    throw new StateMachineConfigurationException($"State '{node.State}' has more than one rule for trigger '{group.Key}' and at least one is unguarded.");
            }

            foreach (var rule in node.Rules) {
                if (payloadTypes.TryGetValue(rule.Trigger, out var existing) && existing != rule.PayloadType)
                    throw new StateMachineConfigurationException($"Trigger '{rule.Trigger}' is used with different payload types.");

                payloadTypes[rule.Trigger] = rule.PayloadType;
            }
        }

        var definition = new StateMachineDefinition<TState, TTrigger, TContext>(
            new(Name, InitialState, _order.ToArray(), _nodes.ToDictionary(p => p.Key, p => p.Value.Copy()), _transitionHooks.ToArray(), _observers.ToArray(), payloadTypes,
                _clock, _unhandledPolicy, _unhandledCallback, _guardReentrancy));

        if (!_strict)
            return definition;

        var analysis = definition.Analyze();
        if (!analysis.IsClean) {
            throw new StateMachineConfigurationException(
                $"State machine '{Name}' failed strict validation. Unreachable: [{string.Join(", ", analysis.UnreachableStates)}]. Dead ends: [{string.Join(", ", analysis.DeadEndStates)}].");
        }

        return definition;
    }

    internal StateMachineBuilder<TState, TTrigger, TContext> AddTransitionHook(Hook<TState, TTrigger, TContext> hook)
    {
        _transitionHooks.Add(hook);
        return this;
    }

    internal StateNode<TState, TTrigger, TContext> GetNode(TState state)
    {
        if (_nodes.TryGetValue(state, out var node))
            return node;

        node = new(state);
        _nodes[state] = node;
        _order.Add(state);
        return node;
    }

    private bool IsDirectChild(TState child, TState parent)
        => _nodes[child].HasParent && EqualityComparer<TState>.Default.Equals(_nodes[child].Parent, parent);

    private void EnsureNoCycle(TState state)
    {
        var seen = new HashSet<TState> { state };
        var node = _nodes[state];
        while (node.HasParent) {
            if (!seen.Add(node.Parent))
                throw new StateMachineConfigurationException($"State '{state}' is part of a substate cycle.");

            node = _nodes[node.Parent];
        }
    }
}
