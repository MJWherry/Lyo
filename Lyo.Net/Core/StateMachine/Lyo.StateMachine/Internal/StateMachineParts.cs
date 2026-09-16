namespace Lyo.StateMachine.Internal;

internal sealed class Hook<TState, TTrigger, TContext>(
    Action<TransitionInfo<TState, TTrigger, TContext>>? sync,
    Func<TransitionInfo<TState, TTrigger, TContext>, CancellationToken, Task>? async,
    string? name = null)
    where TState : notnull where TTrigger : notnull
{
    public bool IsAsync => async is not null;

    public string? Name { get; } = name;

    public Task InvokeAsync(TransitionInfo<TState, TTrigger, TContext> info, CancellationToken ct)
    {
        if (async is not null)
            return async(info, ct);

        sync!(info);
        return Task.CompletedTask;
    }

    public void Invoke(TransitionInfo<TState, TTrigger, TContext> info) => sync!(info);
}

internal enum RuleKind
{
    Permit,
    Reentry,
    Internal,
    Ignore,
    Dynamic
}

internal sealed class TransitionRule<TState, TTrigger, TContext>
    where TState : notnull where TTrigger : notnull
{
    public required TTrigger Trigger { get; init; }

    public required RuleKind Kind { get; init; }

    public TState Destination { get; init; } = default!;

    public TState[] Destinations { get; init; } = [];

    public Func<TContext, object?, bool>? Guard { get; init; }

    public bool GuardNeedsPayload { get; init; }

    public string? GuardDescription { get; init; }

    public Type? PayloadType { get; init; }

    public Hook<TState, TTrigger, TContext>? Action { get; init; }

    public Func<TContext, object?, TState>? Selector { get; init; }

    public string? SelectorName { get; init; }
}

internal sealed class StateNode<TState, TTrigger, TContext>(TState state)
    where TState : notnull where TTrigger : notnull
{
    public TState State { get; } = state;

    public bool HasParent { get; set; }

    public TState Parent { get; set; } = default!;

    public bool HasInitialSubstate { get; set; }

    public TState InitialSubstate { get; set; } = default!;

    public bool IsFinal { get; set; }

    public List<TransitionRule<TState, TTrigger, TContext>> Rules { get; } = [];

    public List<Hook<TState, TTrigger, TContext>> Entry { get; } = [];

    public List<Hook<TState, TTrigger, TContext>> Exit { get; } = [];

    public List<(TimeSpan After, TTrigger Trigger)> Timers { get; } = [];

    public StateNode<TState, TTrigger, TContext> Copy()
    {
        var copy = new StateNode<TState, TTrigger, TContext>(State) {
            HasParent = HasParent,
            Parent = Parent,
            HasInitialSubstate = HasInitialSubstate,
            InitialSubstate = InitialSubstate,
            IsFinal = IsFinal
        };

        copy.Rules.AddRange(Rules);
        copy.Entry.AddRange(Entry);
        copy.Exit.AddRange(Exit);
        copy.Timers.AddRange(Timers);
        return copy;
    }
}

internal sealed record DefinitionSettings<TState, TTrigger, TContext>(
    string Name,
    TState InitialState,
    TState[] States,
    Dictionary<TState, StateNode<TState, TTrigger, TContext>> Nodes,
    Hook<TState, TTrigger, TContext>[] TransitionHooks,
    IStateMachineObserver<TState, TTrigger>[] Observers,
    Dictionary<TTrigger, Type?> PayloadTypes,
    Func<DateTime> Clock,
    UnhandledTriggerPolicy UnhandledPolicy,
    Action<TContext, TState, TTrigger>? UnhandledCallback,
    bool GuardReentrancy)
    where TState : notnull where TTrigger : notnull;

internal sealed class FiringFrame(object definition, object? context, FiringFrame? previous)
{
    public object Definition { get; } = definition;

    public object? Context { get; } = context;

    public FiringFrame? Previous { get; } = previous;
}

internal readonly struct PathStep<TState, TTrigger, TContext>(Hook<TState, TTrigger, TContext> hook, TState state)
    where TState : notnull where TTrigger : notnull
{
    public Hook<TState, TTrigger, TContext> Hook { get; } = hook;

    public TState State { get; } = state;
}

internal sealed class TransitionPath<TState, TTrigger, TContext>(TState destination, PathStep<TState, TTrigger, TContext>[] exits, PathStep<TState, TTrigger, TContext>[] entries)
    where TState : notnull where TTrigger : notnull
{
    public TState Destination { get; } = destination;

    public PathStep<TState, TTrigger, TContext>[] Exits { get; } = exits;

    public PathStep<TState, TTrigger, TContext>[] Entries { get; } = entries;
}

internal sealed class CompiledRule<TState, TTrigger, TContext>(
    TransitionRule<TState, TTrigger, TContext> rule,
    TransitionPath<TState, TTrigger, TContext>? path,
    Dictionary<TState, TransitionPath<TState, TTrigger, TContext>>? dynamicPaths)
    where TState : notnull where TTrigger : notnull
{
    public TransitionRule<TState, TTrigger, TContext> Rule { get; } = rule;

    public TransitionPath<TState, TTrigger, TContext>? Path { get; } = path;

    public Dictionary<TState, TransitionPath<TState, TTrigger, TContext>>? DynamicPaths { get; } = dynamicPaths;
}

internal sealed class CompiledState<TState, TTrigger, TContext>(
    StateNode<TState, TTrigger, TContext> node,
    TState[] selfAndAncestors,
    Dictionary<TTrigger, CompiledRule<TState, TTrigger, TContext>[]> table,
    TTrigger[] triggerOrder)
    where TState : notnull where TTrigger : notnull
{
    public StateNode<TState, TTrigger, TContext> Node { get; } = node;

    public TState[] SelfAndAncestors { get; } = selfAndAncestors;

    public Dictionary<TTrigger, CompiledRule<TState, TTrigger, TContext>[]> Table { get; } = table;

    public TTrigger[] TriggerOrder { get; } = triggerOrder;

    public bool HasTimer { get; set; }

    public TimeSpan TimerAfter { get; set; }

    public TTrigger TimerTrigger { get; set; } = default!;
}
