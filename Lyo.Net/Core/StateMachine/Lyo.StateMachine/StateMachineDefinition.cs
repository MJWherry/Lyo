using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.StateMachine.Internal;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>An immutable, thread-safe state machine definition built by <see cref="StateMachineBuilder{TState,TTrigger,TContext}" />.</summary>
/// <remarks>
/// The definition holds rules only. Per-entity state lives in a <see cref="StateSnapshot{TState}" /> the caller keeps (in memory for a game, in a store for a job). Firing never
/// mutates the snapshot: a transition or re-entry returns a new one with the version incremented; internal and ignored triggers return the same one. Invalid transitions come back as
/// a <see cref="TransitionResult{TState}" /> rejection; only programming errors throw.
/// <para>
/// Hook order for a transition: exit hooks from the current state up to (not including) the common ancestor, then transition hooks, then entry hooks from below the common
/// ancestor down to the destination (and on into initial substates), then observers. Hook paths are precomputed at build time. Exceptions from guards, hooks, and selectors are
/// wrapped in <see cref="StateMachineHookException" />; cancellation is checked before each hook.
/// </para>
/// <para>Snapshot times and the clock must be UTC.</para>
/// </remarks>
public sealed class StateMachineDefinition<TState, TTrigger, TContext>
    where TState : notnull where TTrigger : notnull
{
    private static readonly EqualityComparer<TState> StateComparer = EqualityComparer<TState>.Default;
    private static readonly double TicksPerTimestamp = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
    private static readonly AsyncLocal<FiringFrame?> Firing = new();

    private readonly Func<DateTime> _clock;
    private readonly Dictionary<TState, CompiledState<TState, TTrigger, TContext>> _compiled;
    private readonly bool _guardReentrancy;
    private readonly TState _initialLeaf;
    private readonly IStateMachineObserver<TState, TTrigger>[] _observers;
    private readonly Dictionary<TTrigger, Type?> _payloadTypes;
    private readonly Hook<TState, TTrigger, TContext>[] _transitionHooks;
    private readonly Action<TContext, TState, TTrigger>? _unhandledCallback;
    private readonly UnhandledTriggerPolicy _unhandledPolicy;

    /// <summary>Machine name. Stores key saved instances by it.</summary>
    public string Name { get; }

    /// <summary>State new instances start in, before drilling into initial substates.</summary>
    public TState InitialState { get; }

    /// <summary>Every known state, in the order first mentioned on the builder.</summary>
    public IReadOnlyList<TState> States { get; }

    /// <summary>True when any hook or internal action is async. Such machines must be fired with <c>FireAsync</c>.</summary>
    public bool HasAsyncHooks { get; }

    internal IReadOnlyDictionary<TState, StateNode<TState, TTrigger, TContext>> Nodes { get; }

    internal IReadOnlyList<Hook<TState, TTrigger, TContext>> TransitionHooks => _transitionHooks;

    internal IReadOnlyDictionary<TTrigger, Type?> PayloadTypes => _payloadTypes;

    internal StateMachineDefinition(DefinitionSettings<TState, TTrigger, TContext> settings)
    {
        Name = settings.Name;
        InitialState = settings.InitialState;
        States = settings.States;
        Nodes = settings.Nodes;
        _transitionHooks = settings.TransitionHooks;
        _observers = settings.Observers;
        _payloadTypes = settings.PayloadTypes;
        _clock = settings.Clock;
        _unhandledPolicy = settings.UnhandledPolicy;
        _unhandledCallback = settings.UnhandledCallback;
        _guardReentrancy = settings.GuardReentrancy;
        _compiled = new(settings.States.Length);
        foreach (var state in settings.States)
            _compiled[state] = CompileState(state);

        ValidateTimers();
        _initialLeaf = Drill(InitialState);
        HasAsyncHooks = _transitionHooks.Any(h => h.IsAsync) ||
            Nodes.Values.Any(n => n.Entry.Any(h => h.IsAsync) || n.Exit.Any(h => h.IsAsync) || n.Rules.Any(r => r.Action?.IsAsync == true));
    }

    /// <summary>A version-0 snapshot in <see cref="InitialState" /> (or its initial substate), with its timer armed.</summary>
    /// <exception cref="StateMachineConfigurationException">The clock returned a non-UTC time.</exception>
    public StateSnapshot<TState> CreateInitialSnapshot()
    {
        var now = Now();
        return new(_initialLeaf, 0, now, DueAt(_compiled[_initialLeaf], now));
    }

    /// <summary>Fires a trigger that takes no payload.</summary>
    /// <remarks>When <see cref="HasAsyncHooks" /> is false this runs synchronously and returns a completed task.</remarks>
    /// <exception cref="ArgumentException">The trigger was registered with a payload, the snapshot state is unknown, or a snapshot time is not UTC.</exception>
    public Task<TransitionResult<TState>> FireAsync(TContext context, StateSnapshot<TState> snapshot, TTrigger trigger, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        return FireAnyAsync(context, snapshot, trigger, null, null, ct);
    }

    /// <summary>Fires a payload trigger. Guards, selectors, internal actions, and <c>OnEntryFrom</c> hooks receive <paramref name="payload" /> typed.</summary>
    /// <exception cref="ArgumentException">The trigger was registered without this payload type, the snapshot state is unknown, or a snapshot time is not UTC.</exception>
    public Task<TransitionResult<TState>> FireAsync<TPayload>(
        TContext context,
        StateSnapshot<TState> snapshot,
        Trigger<TTrigger, TPayload> trigger,
        TPayload payload,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        return FireAnyAsync(context, snapshot, trigger.Value, payload, typeof(TPayload), ct);
    }

    /// <summary>Synchronous <see cref="FireAsync(TContext, StateSnapshot{TState}, TTrigger, CancellationToken)" /> for game loops and other hot paths.</summary>
    /// <exception cref="StateMachineConfigurationException">The definition has async hooks (<see cref="HasAsyncHooks" />).</exception>
    public TransitionResult<TState> Fire(TContext context, StateSnapshot<TState> snapshot, TTrigger trigger)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        EnsureSync();
        return FireCore(context, snapshot, trigger, null, null, CancellationToken.None);
    }

    /// <summary>Synchronous payload form of <see cref="Fire(TContext, StateSnapshot{TState}, TTrigger)" />.</summary>
    /// <exception cref="StateMachineConfigurationException">The definition has async hooks (<see cref="HasAsyncHooks" />).</exception>
    public TransitionResult<TState> Fire<TPayload>(TContext context, StateSnapshot<TState> snapshot, Trigger<TTrigger, TPayload> trigger, TPayload payload)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        EnsureSync();
        return FireCore(context, snapshot, trigger.Value, payload, typeof(TPayload), CancellationToken.None);
    }

    /// <summary>True when <paramref name="trigger" /> would currently be accepted from <paramref name="state" />. Payload guards are not evaluated and count as passing.</summary>
    public bool CanFire(TContext context, TState state, TTrigger trigger)
    {
        var compiled = GetCompiled(state);
        return compiled.Table.TryGetValue(trigger, out var rules) && AnyGuardPassesWithoutPayload(rules, context, state);
    }

    /// <summary>Distinct triggers accepted from <paramref name="state" /> (own rules first, then inherited). Payload guards are not evaluated and count as passing.</summary>
    public IReadOnlyList<TTrigger> GetPermittedTriggers(TContext context, TState state)
    {
        var compiled = GetCompiled(state);
        var permitted = new List<TTrigger>(compiled.TriggerOrder.Length);
        foreach (var trigger in compiled.TriggerOrder) {
            if (AnyGuardPassesWithoutPayload(compiled.Table[trigger], context, state))
                permitted.Add(trigger);
        }

        return permitted;
    }

    /// <summary>True when <paramref name="current" /> is <paramref name="state" /> or one of its substates.</summary>
    public bool IsInState(TState current, TState state) => Array.IndexOf(GetCompiled(current).SelfAndAncestors, state) >= 0;

    /// <summary><paramref name="state" /> and every substate below it, in declaration order. Useful for <see cref="StateInstanceFilter{TState}.States" />.</summary>
    public IReadOnlyList<TState> GetSelfAndDescendants(TState state)
    {
        GetCompiled(state);
        return States.Where(s => Array.IndexOf(_compiled[s].SelfAndAncestors, state) >= 0).ToArray();
    }

    /// <summary>The timer trigger due for <paramref name="snapshot" /> at <paramref name="nowUtc" />, or null when the state has no timer or it is not due yet.</summary>
    /// <remarks>Uses the definition's timers and <see cref="StateSnapshot{TState}.EnteredAtUtc" />, so it stays correct if timers change between revisions.</remarks>
    public StateTimer<TTrigger>? GetDueTrigger(StateSnapshot<TState> snapshot, DateTime nowUtc)
    {
        ArgumentHelpers.ThrowIfNull(snapshot);
        var due = DueAt(GetCompiled(snapshot.State), snapshot.EnteredAtUtc);
        return due is { } at && nowUtc >= at ? new StateTimer<TTrigger>(_compiled[snapshot.State].TimerTrigger, at) : null;
    }

    /// <summary>Finds unreachable states and dead ends (no way out, not final). <see cref="StateMachineBuilder{TState,TTrigger,TContext}.Strict" /> runs this at build.</summary>
    public StateMachineAnalysis<TState> Analyze()
    {
        var reached = new HashSet<TState>();
        var queue = new Queue<TState>();
        Reach(_initialLeaf);
        while (queue.Count > 0) {
            foreach (var rules in _compiled[queue.Dequeue()].Table.Values) {
                foreach (var rule in rules) {
                    if (rule.Path is not null && rule.Rule.Kind == RuleKind.Permit)
                        Reach(rule.Path.Destination);

                    foreach (var path in rule.DynamicPaths?.Values ?? Enumerable.Empty<TransitionPath<TState, TTrigger, TContext>>())
                        Reach(path.Destination);
                }
            }
        }

        var deadEnds = States.Where(s
            => !Nodes[s].IsFinal && !Nodes[s].HasInitialSubstate && !_compiled[s].Table.Values.Any(rs => rs.Any(r => r.Rule.Kind is RuleKind.Permit or RuleKind.Dynamic)));

        return new(States.Where(s => !reached.Contains(s)).ToArray(), deadEnds.ToArray());

        void Reach(TState state)
        {
            if (reached.Contains(state))
                return;

            foreach (var s in _compiled[state].SelfAndAncestors)
                reached.Add(s);

            queue.Enqueue(state);
        }
    }

    /// <summary>Renders the machine as a Mermaid <c>stateDiagram-v2</c>. Substates are nested; guards appear as <c>[description]</c>.</summary>
    public string ToMermaid() => StateMachineDiagram.Mermaid(this);

    /// <summary>Renders the machine as Graphviz DOT. Substates are drawn as clusters.</summary>
    public string ToDot() => StateMachineDiagram.Dot(this);

    /// <summary>Describes the machine as a <see cref="StateMachineDocument" /> for saving to an <see cref="IStateMachineDefinitionStore" />.</summary>
    /// <remarks>
    /// Round-trips exactly for machines built by <see cref="StateMachineDocumentLoader" />. For code-built machines, guard descriptions become guard names (an undescribed guard
    /// is exported as "guard"), and hooks, actions, and selectors without a registry name are left out, so register matching names before loading the result.
    /// </remarks>
    public StateMachineDocument ToDocument(IStateKeyConverter<TState> stateConverter, IStateKeyConverter<TTrigger> triggerConverter, int revision = 0)
    {
        ArgumentHelpers.ThrowIfNull(stateConverter);
        ArgumentHelpers.ThrowIfNull(triggerConverter);
        return new() {
            Name = Name,
            Revision = revision,
            InitialState = stateConverter.ToKey(InitialState),
            OnTransition = Names(_transitionHooks),
            States = States.Select(s => Nodes[s])
                .Select(n => new StateDocument {
                    Name = stateConverter.ToKey(n.State),
                    Parent = n.HasParent ? stateConverter.ToKey(n.Parent) : null,
                    InitialSubstate = n.HasInitialSubstate ? stateConverter.ToKey(n.InitialSubstate) : null,
                    IsFinal = n.IsFinal,
                    OnEntry = Names(n.Entry),
                    OnExit = Names(n.Exit),
                    Timers = n.Timers.Select(t => new TimerDocument { After = t.After, Trigger = triggerConverter.ToKey(t.Trigger) }).ToArray(),
                    Transitions = n.Rules.Select(r => new TransitionDocument {
                            Kind = r.Kind switch {
                                RuleKind.Permit => TransitionDocumentKind.Permit,
                                RuleKind.Reentry => TransitionDocumentKind.Reentry,
                                RuleKind.Internal => TransitionDocumentKind.Internal,
                                RuleKind.Ignore => TransitionDocumentKind.Ignore,
                                var _ => TransitionDocumentKind.Dynamic
                            },
                            Trigger = triggerConverter.ToKey(r.Trigger),
                            Destination = r.Kind == RuleKind.Permit ? stateConverter.ToKey(r.Destination) : null,
                            Destinations = r.Destinations.Select(stateConverter.ToKey).ToArray(),
                            Guard = r.Guard is null ? null : r.GuardDescription ?? "guard",
                            Action = r.Action?.Name,
                            Selector = r.SelectorName
                        })
                        .ToArray()
                })
                .ToArray()
        };

        static string[] Names(IEnumerable<Hook<TState, TTrigger, TContext>> hooks) => hooks.Where(h => h.Name is not null).Select(h => h.Name!).ToArray();
    }

    internal StateNode<TState, TTrigger, TContext> Node(TState state) => Nodes[state];

    private static bool IsWrappable(Exception ex) => ex is not (OperationCanceledException or StateMachineReentrancyException);

    private Task<TransitionResult<TState>> FireAnyAsync(
        TContext context,
        StateSnapshot<TState> snapshot,
        TTrigger trigger,
        object? payload,
        Type? payloadType,
        CancellationToken ct)
    {
        if (HasAsyncHooks)
            return FireCoreAsync(context, snapshot, trigger, payload, payloadType, ct);

        try {
            return Task.FromResult(FireCore(context, snapshot, trigger, payload, payloadType, ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            return Task.FromCanceled<TransitionResult<TState>>(ct);
        }
        catch (Exception ex) {
            return Task.FromException<TransitionResult<TState>>(ex);
        }
    }

    private TransitionResult<TState> FireCore(TContext context, StateSnapshot<TState> snapshot, TTrigger trigger, object? payload, Type? payloadType, CancellationToken ct)
    {
        var state = Begin(snapshot, trigger, payloadType, out var started);
        var previous = EnterFiring(context);
        try {
            ct.ThrowIfCancellationRequested();
            var compiled = Match(state, context, snapshot.State, trigger, payload, out var rejection);
            if (compiled is null)
                return Reject(snapshot, trigger, rejection);

            var source = snapshot.State;
            switch (compiled.Rule.Kind) {
                case RuleKind.Ignore:
                    return Accept(snapshot, trigger, TransitionKind.Ignored, started);
                case RuleKind.Internal:
                    RunHook(compiled.Rule.Action!, new(context, source, source, trigger, payload, TransitionKind.Internal), "internal action", source);
                    return Accept(snapshot, trigger, TransitionKind.Internal, started);
            }

            var path = ResolvePath(compiled, context, source, trigger, payload);
            var info = new TransitionInfo<TState, TTrigger, TContext>(context, source, path.Destination, trigger, payload, KindOf(compiled.Rule));
            foreach (var step in path.Exits) {
                ct.ThrowIfCancellationRequested();
                RunHook(step.Hook, info, "exit hook", step.State);
            }

            foreach (var hook in _transitionHooks) {
                ct.ThrowIfCancellationRequested();
                RunHook(hook, info, "transition hook", source);
            }

            foreach (var step in path.Entries) {
                ct.ThrowIfCancellationRequested();
                RunHook(step.Hook, info, "entry hook", step.State);
            }

            return Complete(snapshot, info, started);
        }
        finally {
            ExitFiring(previous);
        }
    }

    private async Task<TransitionResult<TState>> FireCoreAsync(
        TContext context,
        StateSnapshot<TState> snapshot,
        TTrigger trigger,
        object? payload,
        Type? payloadType,
        CancellationToken ct)
    {
        var state = Begin(snapshot, trigger, payloadType, out var started);
        var previous = EnterFiring(context);
        try {
            ct.ThrowIfCancellationRequested();
            var compiled = Match(state, context, snapshot.State, trigger, payload, out var rejection);
            if (compiled is null)
                return Reject(snapshot, trigger, rejection);

            var source = snapshot.State;
            switch (compiled.Rule.Kind) {
                case RuleKind.Ignore:
                    return Accept(snapshot, trigger, TransitionKind.Ignored, started);
                case RuleKind.Internal:
                    await RunHookAsync(compiled.Rule.Action!, new(context, source, source, trigger, payload, TransitionKind.Internal), "internal action", source, ct)
                        .ConfigureAwait(false);

                    return Accept(snapshot, trigger, TransitionKind.Internal, started);
            }

            var path = ResolvePath(compiled, context, source, trigger, payload);
            var info = new TransitionInfo<TState, TTrigger, TContext>(context, source, path.Destination, trigger, payload, KindOf(compiled.Rule));
            foreach (var step in path.Exits)
                await RunHookAsync(step.Hook, info, "exit hook", step.State, ct).ConfigureAwait(false);

            foreach (var hook in _transitionHooks)
                await RunHookAsync(hook, info, "transition hook", source, ct).ConfigureAwait(false);

            foreach (var step in path.Entries)
                await RunHookAsync(step.Hook, info, "entry hook", step.State, ct).ConfigureAwait(false);

            return Complete(snapshot, info, started);
        }
        finally {
            ExitFiring(previous);
        }
    }

    private static TransitionKind KindOf(TransitionRule<TState, TTrigger, TContext> rule) => rule.Kind == RuleKind.Reentry ? TransitionKind.Reentry : TransitionKind.Transition;

    private CompiledState<TState, TTrigger, TContext> Begin(StateSnapshot<TState> snapshot, TTrigger trigger, Type? payloadType, out long started)
    {
        ArgumentHelpers.ThrowIfNull(snapshot);
        if (snapshot.EnteredAtUtc.Kind != DateTimeKind.Utc || snapshot.DueAtUtc is { Kind: not DateTimeKind.Utc })
            throw new ArgumentException($"Snapshot times for state machine '{Name}' must be UTC.", nameof(snapshot));

        EnsurePayload(trigger, payloadType);
        var state = GetCompiled(snapshot.State);
        started = _observers.Length > 0 ? Stopwatch.GetTimestamp() : 0;
        return state;
    }

    private FiringFrame? EnterFiring(TContext context)
    {
        if (!_guardReentrancy)
            return null;

        var current = Firing.Value;
        for (var frame = current; frame is not null; frame = frame.Previous) {
            if (ReferenceEquals(frame.Definition, this) && frame.Context is not null && ReferenceEquals(frame.Context, context))
                throw new StateMachineReentrancyException($"State machine '{Name}' was fired again for the same context from inside one of its own hooks.");
        }

        Firing.Value = new(this, context, current);
        return current;
    }

    private void ExitFiring(FiringFrame? previous)
    {
        if (_guardReentrancy)
            Firing.Value = previous;
    }

    private CompiledRule<TState, TTrigger, TContext>? Match(
        CompiledState<TState, TTrigger, TContext> state,
        TContext context,
        TState source,
        TTrigger trigger,
        object? payload,
        out TransitionRejection rejection)
    {
        if (!state.Table.TryGetValue(trigger, out var rules)) {
            if (_unhandledPolicy == UnhandledTriggerPolicy.Throw)
                throw new UnhandledTriggerException($"State machine '{Name}' has no rule for trigger '{trigger}' in state '{source}'.");

            _unhandledCallback?.Invoke(context, source, trigger);
            rejection = TransitionRejection.NoTransition;
            return null;
        }

        foreach (var rule in rules) {
            if (rule.Rule.Guard is null || GuardPasses(rule.Rule, context, payload, source)) {
                rejection = TransitionRejection.None;
                return rule;
            }
        }

        rejection = TransitionRejection.GuardFailed;
        return null;
    }

    private bool GuardPasses(TransitionRule<TState, TTrigger, TContext> rule, TContext context, object? payload, TState source)
    {
        try {
            return rule.Guard!(context, payload);
        }
        catch (Exception ex) when (IsWrappable(ex)) {
            throw HookFailure("guard", source, rule.Trigger, ex);
        }
    }

    private bool AnyGuardPassesWithoutPayload(CompiledRule<TState, TTrigger, TContext>[] rules, TContext context, TState source)
    {
        foreach (var rule in rules) {
            if (rule.Rule.Guard is null || rule.Rule.GuardNeedsPayload || GuardPasses(rule.Rule, context, null, source))
                return true;
        }

        return false;
    }

    private TransitionPath<TState, TTrigger, TContext> ResolvePath(
        CompiledRule<TState, TTrigger, TContext> compiled,
        TContext context,
        TState source,
        TTrigger trigger,
        object? payload)
    {
        if (compiled.Path is not null)
            return compiled.Path;

        TState destination;
        try {
            destination = compiled.Rule.Selector!(context, payload);
        }
        catch (Exception ex) when (IsWrappable(ex)) {
            throw HookFailure("selector", source, trigger, ex);
        }

        if (destination is null || !compiled.DynamicPaths!.TryGetValue(destination, out var path)) {
            throw new InvalidOperationException(
                $"State machine '{Name}' dynamic rule for trigger '{trigger}' in state '{source}' selected '{destination}', which is not one of its possible destinations.");
        }

        return path;
    }

    private void RunHook(Hook<TState, TTrigger, TContext> hook, TransitionInfo<TState, TTrigger, TContext> info, string phase, TState state)
    {
        try {
            hook.Invoke(info);
        }
        catch (Exception ex) when (IsWrappable(ex)) {
            throw HookFailure(phase, state, info.Trigger, ex);
        }
    }

    private Task RunHookAsync(Hook<TState, TTrigger, TContext> hook, TransitionInfo<TState, TTrigger, TContext> info, string phase, TState state, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Task task;
        try {
            task = hook.InvokeAsync(info, ct);
        }
        catch (Exception ex) when (IsWrappable(ex)) {
            throw HookFailure(phase, state, info.Trigger, ex);
        }

        return task.Status == TaskStatus.RanToCompletion ? Task.CompletedTask : AwaitHookAsync(task, phase, state, info.Trigger);
    }

    private async Task AwaitHookAsync(Task task, string phase, TState state, TTrigger trigger)
    {
        try {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex) when (IsWrappable(ex)) {
            throw HookFailure(phase, state, trigger, ex);
        }
    }

    private StateMachineHookException HookFailure(string phase, TState state, TTrigger trigger, Exception inner)
        => new($"State machine '{Name}' {phase} for state '{state}' failed on trigger '{trigger}': {inner.Message}", inner);

    private TransitionResult<TState> Complete(StateSnapshot<TState> snapshot, TransitionInfo<TState, TTrigger, TContext> info, long started)
    {
        var now = Now();
        var next = new StateSnapshot<TState>(info.Destination, snapshot.Version + 1, now, DueAt(_compiled[info.Destination], now), snapshot.DataJson);
        NotifyTransitioned(info.Source, info.Destination, info.Trigger, next.Version, info.Kind, now, started);
        return new(TransitionRejection.None, info.Source, next, info.Kind);
    }

    private TransitionResult<TState> Accept(StateSnapshot<TState> snapshot, TTrigger trigger, TransitionKind kind, long started)
    {
        if (_observers.Length > 0)
            NotifyTransitioned(snapshot.State, snapshot.State, trigger, snapshot.Version, kind, Now(), started);

        return new(TransitionRejection.None, snapshot.State, snapshot, kind);
    }

    private TransitionResult<TState> Reject(StateSnapshot<TState> snapshot, TTrigger trigger, TransitionRejection rejection)
    {
        if (_observers.Length > 0) {
            var rejected = new StateTransitionRejected<TState, TTrigger>(Name, snapshot.State, trigger, rejection);
            foreach (var observer in _observers)
                observer.OnRejected(rejected);
        }

        return new(rejection, snapshot.State, snapshot);
    }

    private void NotifyTransitioned(TState source, TState destination, TTrigger trigger, long version, TransitionKind kind, DateTime occurredAtUtc, long started)
    {
        if (_observers.Length == 0)
            return;

        var duration = TimeSpan.FromTicks((long)((Stopwatch.GetTimestamp() - started) * TicksPerTimestamp));
        var transitioned = new StateTransitioned<TState, TTrigger>(Name, source, destination, trigger, version, kind, occurredAtUtc, duration);
        foreach (var observer in _observers)
            observer.OnTransitioned(transitioned);
    }

    private DateTime Now()
    {
        var now = _clock();
        if (now.Kind != DateTimeKind.Utc)
            throw new StateMachineConfigurationException($"The clock for state machine '{Name}' returned a {now.Kind} time. It must return UTC.");

        return now;
    }

    private static DateTime? DueAt(CompiledState<TState, TTrigger, TContext> state, DateTime enteredAtUtc) => state.HasTimer ? enteredAtUtc + state.TimerAfter : null;

    private void EnsureSync()
    {
        if (HasAsyncHooks)
            throw new StateMachineConfigurationException($"State machine '{Name}' has async hooks. Use FireAsync.");
    }

    private void EnsurePayload(TTrigger trigger, Type? payloadType)
    {
        if (!_payloadTypes.TryGetValue(trigger, out var expected) || expected == payloadType)
            return;

        var message = expected is null ? $"Trigger '{trigger}' takes no payload." : $"Trigger '{trigger}' requires a payload of type {expected.Name}.";
        throw new ArgumentException(message, nameof(trigger));
    }

    private CompiledState<TState, TTrigger, TContext> GetCompiled(TState state)
    {
        ArgumentHelpers.ThrowIfNull(state);
        return _compiled.TryGetValue(state, out var compiled) ? compiled : throw new ArgumentException($"State '{state}' is not part of state machine '{Name}'.", nameof(state));
    }

    private CompiledState<TState, TTrigger, TContext> CompileState(TState state)
    {
        var chain = Chain(state);
        var table = new Dictionary<TTrigger, List<CompiledRule<TState, TTrigger, TContext>>>();
        var order = new List<TTrigger>();
        foreach (var owner in chain) {
            foreach (var rule in Nodes[owner].Rules) {
                if (!table.TryGetValue(rule.Trigger, out var list)) {
                    table[rule.Trigger] = list = [];
                    order.Add(rule.Trigger);
                }

                list.Add(CompileRule(state, rule));
            }
        }

        var node = Nodes[state];
        var compiled = new CompiledState<TState, TTrigger, TContext>(node, chain, table.ToDictionary(p => p.Key, p => p.Value.ToArray()), order.ToArray());
        if (node.Timers.Count > 0) {
            var earliest = node.Timers.OrderBy(t => t.After).First();
            compiled.HasTimer = true;
            compiled.TimerAfter = earliest.After;
            compiled.TimerTrigger = earliest.Trigger;
        }

        return compiled;
    }

    private CompiledRule<TState, TTrigger, TContext> CompileRule(TState source, TransitionRule<TState, TTrigger, TContext> rule)
        => rule.Kind switch {
            RuleKind.Permit => new(rule, BuildPath(source, rule.Destination), null),
            RuleKind.Reentry => new(rule, new(source, Steps([source], false), Steps([source], true)), null),
            RuleKind.Dynamic => new(rule, null, rule.Destinations.Distinct().ToDictionary(d => d, d => BuildPath(source, d))),
            var _ => new(rule, null, null)
        };

    private TransitionPath<TState, TTrigger, TContext> BuildPath(TState source, TState destination)
    {
        var sourcePath = Chain(source).ToList();
        var destinationPath = Chain(destination).ToList();
        var commonInSource = sourcePath.FindIndex(s => destinationPath.Contains(s, StateComparer));
        List<TState> exits, entries;
        if (commonInSource < 0) {
            exits = sourcePath;
            entries = destinationPath.AsEnumerable().Reverse().ToList();
        }
        else {
            exits = sourcePath.Take(commonInSource).ToList();
            entries = destinationPath.Take(destinationPath.IndexOf(sourcePath[commonInSource])).Reverse().ToList();
        }

        var leaf = destination;
        while (Nodes[leaf].HasInitialSubstate) {
            leaf = Nodes[leaf].InitialSubstate;
            entries.Add(leaf);
        }

        return new(leaf, Steps(exits, false), Steps(entries, true));
    }

    private PathStep<TState, TTrigger, TContext>[] Steps(IEnumerable<TState> states, bool entry)
        => states.SelectMany(s => (entry ? Nodes[s].Entry : Nodes[s].Exit).Select(h => new PathStep<TState, TTrigger, TContext>(h, s))).ToArray();

    private TState[] Chain(TState state)
    {
        var chain = new List<TState> { state };
        var node = Nodes[state];
        while (node.HasParent) {
            chain.Add(node.Parent);
            node = Nodes[node.Parent];
        }

        return chain.ToArray();
    }

    private TState Drill(TState state)
    {
        while (Nodes[state].HasInitialSubstate)
            state = Nodes[state].InitialSubstate;

        return state;
    }

    private void ValidateTimers()
    {
        foreach (var state in States) {
            foreach (var (_, trigger) in Nodes[state].Timers) {
                if (!_compiled[state].Table.ContainsKey(trigger))
                    throw new StateMachineConfigurationException($"Timer on state '{state}' fires '{trigger}', which is not permitted from that state.");

                if (_payloadTypes.TryGetValue(trigger, out var payloadType) && payloadType is not null)
                    throw new StateMachineConfigurationException(
                        $"Timer on state '{state}' fires '{trigger}', which takes a {payloadType.Name} payload. Timer triggers cannot take a payload.");
            }
        }
    }
}