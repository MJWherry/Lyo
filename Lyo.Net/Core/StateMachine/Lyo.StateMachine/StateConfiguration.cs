using Lyo.Exceptions;
using Lyo.StateMachine.Internal;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>Configures one state: its outgoing rules, its parent and initial substate, timers, and entry/exit hooks. Obtain from <c>StateMachineBuilder.Configure</c>.</summary>
/// <remarks>
/// A guard is a pure check on the context (and payload). When several guarded rules exist for one trigger, the first registered rule whose guard passes wins.
/// Rules on a substate are tried before the parent's, so a child can override a parent transition. Guard descriptions double as guard names in
/// <see cref="StateMachineDocument" /> exports.
/// </remarks>
public sealed class StateConfiguration<TState, TTrigger, TContext>
    where TState : notnull where TTrigger : notnull
{
    private readonly StateMachineBuilder<TState, TTrigger, TContext> _builder;
    private readonly StateNode<TState, TTrigger, TContext> _node;

    internal StateConfiguration(StateMachineBuilder<TState, TTrigger, TContext> builder, StateNode<TState, TTrigger, TContext> node)
    {
        _builder = builder;
        _node = node;
    }

    /// <summary>The state being configured.</summary>
    public TState State => _node.State;

    /// <summary>On <paramref name="trigger" />, move to <paramref name="destination" />. Use <see cref="PermitReentry" /> to stay in the same state.</summary>
    public StateConfiguration<TState, TTrigger, TContext> Permit(TTrigger trigger, TState destination)
        => AddRule(new() { Trigger = trigger, Kind = RuleKind.Permit, Destination = destination });

    /// <summary>On <paramref name="trigger" />, move to <paramref name="destination" /> when <paramref name="guard" /> returns true.</summary>
    /// <param name="trigger">Trigger to accept.</param>
    /// <param name="destination">State to move to.</param>
    /// <param name="guard">Pure check on the context. Evaluated on every fire and by <c>CanFire</c>/<c>GetPermittedTriggers</c>.</param>
    /// <param name="guardDescription">Short label shown in diagrams, for example "has key".</param>
    public StateConfiguration<TState, TTrigger, TContext> PermitIf(TTrigger trigger, TState destination, Func<TContext, bool> guard, string? guardDescription = null)
    {
        ArgumentHelpers.ThrowIfNull(guard);
        return AddRule(new() { Trigger = trigger, Kind = RuleKind.Permit, Destination = destination, Guard = (c, _) => guard(c), GuardDescription = guardDescription });
    }

    /// <summary>On a payload trigger, move to <paramref name="destination" />. The trigger must then be fired with a <typeparamref name="TPayload" />.</summary>
    public StateConfiguration<TState, TTrigger, TContext> Permit<TPayload>(Trigger<TTrigger, TPayload> trigger, TState destination)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        return AddRule(new() { Trigger = trigger.Value, Kind = RuleKind.Permit, Destination = destination, PayloadType = typeof(TPayload) });
    }

    /// <summary>On a payload trigger, move to <paramref name="destination" /> when <paramref name="guard" /> accepts the context and payload.</summary>
    /// <remarks><c>GetPermittedTriggers</c> and <c>CanFire</c> cannot run payload guards, so they report these rules as permitted.</remarks>
    public StateConfiguration<TState, TTrigger, TContext> PermitIf<TPayload>(
        Trigger<TTrigger, TPayload> trigger,
        TState destination,
        Func<TContext, TPayload, bool> guard,
        string? guardDescription = null)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        ArgumentHelpers.ThrowIfNull(guard);
        return AddRule(new() {
            Trigger = trigger.Value,
            Kind = RuleKind.Permit,
            Destination = destination,
            Guard = (c, p) => guard(c, (TPayload)p!),
            GuardNeedsPayload = true,
            GuardDescription = guardDescription,
            PayloadType = typeof(TPayload)
        });
    }

    /// <summary>On <paramref name="trigger" />, exit and re-enter this state. Exit and entry hooks run, timers re-arm, and the version increments.</summary>
    public StateConfiguration<TState, TTrigger, TContext> PermitReentry(TTrigger trigger) => AddRule(new() { Trigger = trigger, Kind = RuleKind.Reentry, Destination = State });

    /// <summary>Guarded form of <see cref="PermitReentry" />.</summary>
    public StateConfiguration<TState, TTrigger, TContext> PermitReentryIf(TTrigger trigger, Func<TContext, bool> guard, string? guardDescription = null)
    {
        ArgumentHelpers.ThrowIfNull(guard);
        return AddRule(new() { Trigger = trigger, Kind = RuleKind.Reentry, Destination = State, Guard = (c, _) => guard(c), GuardDescription = guardDescription });
    }

    /// <summary>
    /// On <paramref name="trigger" />, move to the state <paramref name="selector" /> returns. It must return one of <paramref name="possibleDestinations" />, which
    /// drive diagrams, validation, and precomputed hook paths.
    /// </summary>
    /// <exception cref="StateMachineConfigurationException">No possible destination was given.</exception>
    public StateConfiguration<TState, TTrigger, TContext> PermitDynamic(TTrigger trigger, Func<TContext, TState> selector, params TState[] possibleDestinations)
    {
        ArgumentHelpers.ThrowIfNull(selector);
        return AddRule(new() { Trigger = trigger, Kind = RuleKind.Dynamic, Destinations = possibleDestinations, Selector = (c, _) => selector(c) });
    }

    /// <summary>Payload form of <see cref="PermitDynamic(TTrigger, Func{TContext, TState}, TState[])" />: the selector also receives the typed payload.</summary>
    public StateConfiguration<TState, TTrigger, TContext> PermitDynamic<TPayload>(
        Trigger<TTrigger, TPayload> trigger,
        Func<TContext, TPayload, TState> selector,
        params TState[] possibleDestinations)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        ArgumentHelpers.ThrowIfNull(selector);
        return AddRule(new() {
            Trigger = trigger.Value,
            Kind = RuleKind.Dynamic,
            Destinations = possibleDestinations,
            Selector = (c, p) => selector(c, (TPayload)p!),
            PayloadType = typeof(TPayload)
        });
    }

    /// <summary>Accepts <paramref name="trigger" /> and does nothing: no hooks, same snapshot, <see cref="TransitionKind.Ignored" />.</summary>
    public StateConfiguration<TState, TTrigger, TContext> Ignore(TTrigger trigger) => AddRule(new() { Trigger = trigger, Kind = RuleKind.Ignore });

    /// <summary>On <paramref name="trigger" />, run <paramref name="action" /> without exiting or entering. The snapshot and version stay the same.</summary>
    public StateConfiguration<TState, TTrigger, TContext> InternalTransition(TTrigger trigger, Action<TransitionInfo<TState, TTrigger, TContext>> action)
    {
        ArgumentHelpers.ThrowIfNull(action);
        return AddRule(new() { Trigger = trigger, Kind = RuleKind.Internal, Action = new(action, null) });
    }

    /// <summary>Async form of <see cref="InternalTransition(TTrigger, Action{TransitionInfo{TState, TTrigger, TContext}})" />.</summary>
    public StateConfiguration<TState, TTrigger, TContext> InternalTransitionAsync(TTrigger trigger, Func<TransitionInfo<TState, TTrigger, TContext>, CancellationToken, Task> action)
    {
        ArgumentHelpers.ThrowIfNull(action);
        return AddRule(new() { Trigger = trigger, Kind = RuleKind.Internal, Action = new(null, action) });
    }

    /// <summary>Guarded internal transition. When the guard fails the result is <see cref="TransitionRejection.GuardFailed" />.</summary>
    public StateConfiguration<TState, TTrigger, TContext> InternalTransitionIf(
        TTrigger trigger,
        Func<TContext, bool> guard,
        Action<TransitionInfo<TState, TTrigger, TContext>> action,
        string? guardDescription = null)
    {
        ArgumentHelpers.ThrowIfNull(guard);
        ArgumentHelpers.ThrowIfNull(action);
        return AddRule(new() { Trigger = trigger, Kind = RuleKind.Internal, Action = new(action, null), Guard = (c, _) => guard(c), GuardDescription = guardDescription });
    }

    /// <summary>Internal transition on a payload trigger; the action receives the typed payload.</summary>
    public StateConfiguration<TState, TTrigger, TContext> InternalTransition<TPayload>(
        Trigger<TTrigger, TPayload> trigger,
        Action<TransitionInfo<TState, TTrigger, TContext>, TPayload> action)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        ArgumentHelpers.ThrowIfNull(action);
        return AddRule(new() { Trigger = trigger.Value, Kind = RuleKind.Internal, Action = new(info => action(info, (TPayload)info.Payload!), null), PayloadType = typeof(TPayload) });
    }

    /// <summary>Async form of <see cref="InternalTransition{TPayload}" />.</summary>
    public StateConfiguration<TState, TTrigger, TContext> InternalTransitionAsync<TPayload>(
        Trigger<TTrigger, TPayload> trigger,
        Func<TransitionInfo<TState, TTrigger, TContext>, TPayload, CancellationToken, Task> action)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        ArgumentHelpers.ThrowIfNull(action);
        return AddRule(new() {
            Trigger = trigger.Value,
            Kind = RuleKind.Internal,
            Action = new(null, (info, ct) => action(info, (TPayload)info.Payload!, ct)),
            PayloadType = typeof(TPayload)
        });
    }

    /// <summary>
    /// Fires <paramref name="trigger" /> once the state has been current for <paramref name="after" />. The earliest timer sets
    /// <see cref="StateSnapshot{TState}.DueAtUtc" />; the host drives it with <c>GetDueTrigger</c> or <c>StateMachineInstance.FireDueAsync</c>.
    /// </summary>
    /// <remarks>The trigger must be permitted from this state and take no payload; <c>Build</c> checks both. Timers are not inherited by substates.</remarks>
    public StateConfiguration<TState, TTrigger, TContext> After(TimeSpan after, TTrigger trigger)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        if (after <= TimeSpan.Zero)
            throw new StateMachineConfigurationException($"Timer on state '{State}' for '{trigger}' must be positive.");

        _node.Timers.Add((after, trigger));
        return this;
    }

    /// <summary>Makes this state a child of <paramref name="parent" />. It inherits the parent's transitions and counts as being "in" the parent.</summary>
    public StateConfiguration<TState, TTrigger, TContext> SubstateOf(TState parent)
    {
        ArgumentHelpers.ThrowIfNull(parent);
        _builder.GetNode(parent);
        _node.HasParent = true;
        _node.Parent = parent;
        return this;
    }

    /// <summary>When this state is a destination, continue into <paramref name="child" /> (recursively). The child must be a direct substate.</summary>
    public StateConfiguration<TState, TTrigger, TContext> InitialSubstate(TState child)
    {
        ArgumentHelpers.ThrowIfNull(child);
        _builder.GetNode(child);
        _node.HasInitialSubstate = true;
        _node.InitialSubstate = child;
        return this;
    }

    /// <summary>Marks a terminal state. Final states are allowed to have no way out in <c>Analyze</c> and <c>Strict</c>.</summary>
    public StateConfiguration<TState, TTrigger, TContext> AsFinal()
    {
        _node.IsFinal = true;
        return this;
    }

    /// <summary>Runs when this state is entered (including re-entry). Not run when moving between this state's own substates.</summary>
    public StateConfiguration<TState, TTrigger, TContext> OnEntry(Action<TransitionInfo<TState, TTrigger, TContext>> action)
    {
        ArgumentHelpers.ThrowIfNull(action);
        return AddEntry(new(action, null));
    }

    /// <summary>Async form of <see cref="OnEntry" />. Machines with async hooks can only be fired with <c>FireAsync</c>.</summary>
    public StateConfiguration<TState, TTrigger, TContext> OnEntryAsync(Func<TransitionInfo<TState, TTrigger, TContext>, CancellationToken, Task> action)
    {
        ArgumentHelpers.ThrowIfNull(action);
        return AddEntry(new(null, action));
    }

    /// <summary>Runs on entry only when the state was entered through <paramref name="trigger" />, with its payload typed.</summary>
    public StateConfiguration<TState, TTrigger, TContext> OnEntryFrom<TPayload>(Trigger<TTrigger, TPayload> trigger, Action<TransitionInfo<TState, TTrigger, TContext>, TPayload> action)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        ArgumentHelpers.ThrowIfNull(action);
        return OnEntry(info => {
            if (EqualityComparer<TTrigger>.Default.Equals(info.Trigger, trigger.Value) && info.Payload is TPayload payload)
                action(info, payload);
        });
    }

    /// <summary>Async form of <see cref="OnEntryFrom{TPayload}" />.</summary>
    public StateConfiguration<TState, TTrigger, TContext> OnEntryFromAsync<TPayload>(
        Trigger<TTrigger, TPayload> trigger,
        Func<TransitionInfo<TState, TTrigger, TContext>, TPayload, CancellationToken, Task> action)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        ArgumentHelpers.ThrowIfNull(action);
        return OnEntryAsync((info, ct) => EqualityComparer<TTrigger>.Default.Equals(info.Trigger, trigger.Value) && info.Payload is TPayload payload
            ? action(info, payload, ct)
            : Task.CompletedTask);
    }

    /// <summary>Runs when this state is exited (including re-entry). Not run when moving between this state's own substates.</summary>
    public StateConfiguration<TState, TTrigger, TContext> OnExit(Action<TransitionInfo<TState, TTrigger, TContext>> action)
    {
        ArgumentHelpers.ThrowIfNull(action);
        return AddExit(new(action, null));
    }

    /// <summary>Async form of <see cref="OnExit" />.</summary>
    public StateConfiguration<TState, TTrigger, TContext> OnExitAsync(Func<TransitionInfo<TState, TTrigger, TContext>, CancellationToken, Task> action)
    {
        ArgumentHelpers.ThrowIfNull(action);
        return AddExit(new(null, action));
    }

    /// <summary>Moves on to configuring another state.</summary>
    public StateConfiguration<TState, TTrigger, TContext> Configure(TState state) => _builder.Configure(state);

    /// <summary>Builds the definition. Same as <see cref="StateMachineBuilder{TState,TTrigger,TContext}.Build" />.</summary>
    /// <exception cref="StateMachineConfigurationException">The configuration is invalid.</exception>
    public StateMachineDefinition<TState, TTrigger, TContext> Build() => _builder.Build();

    internal StateConfiguration<TState, TTrigger, TContext> AddEntry(Hook<TState, TTrigger, TContext> hook)
    {
        _node.Entry.Add(hook);
        return this;
    }

    internal StateConfiguration<TState, TTrigger, TContext> AddExit(Hook<TState, TTrigger, TContext> hook)
    {
        _node.Exit.Add(hook);
        return this;
    }

    internal StateConfiguration<TState, TTrigger, TContext> AddRule(TransitionRule<TState, TTrigger, TContext> rule)
    {
        ArgumentHelpers.ThrowIfNull(rule.Trigger);
        switch (rule.Kind) {
            case RuleKind.Permit:
                ArgumentHelpers.ThrowIfNull(rule.Destination);
                if (EqualityComparer<TState>.Default.Equals(rule.Destination, State))
                    throw new StateMachineConfigurationException($"State '{State}' permits '{rule.Trigger}' to itself. Use PermitReentry instead.");

                _builder.GetNode(rule.Destination);
                break;
            case RuleKind.Dynamic:
                if (rule.Destinations.Length == 0)
                    throw new StateMachineConfigurationException($"State '{State}' has a dynamic rule for '{rule.Trigger}' with no possible destinations.");

                foreach (var destination in rule.Destinations)
                    _builder.GetNode(destination);

                break;
        }

        _node.Rules.Add(rule);
        return this;
    }
}
