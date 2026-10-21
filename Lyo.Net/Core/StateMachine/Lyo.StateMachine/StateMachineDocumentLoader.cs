using Lyo.Exceptions;
using Lyo.StateMachine.Internal;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>Builds a <see cref="StateMachineDefinition{TState,TTrigger,TContext}" /> from a <see cref="StateMachineDocument" />, resolving names through a registry.</summary>
/// <remarks>
/// Every problem in the document (unknown states or triggers, missing guard/action/selector names, payload mismatches) is collected and reported together in one
/// <see cref="StateMachineConfigurationException" />, then the normal <c>Build</c> validation runs. Loading is not cheap; cache the result (
/// <see cref="StateMachineDefinitionCache{TState,TTrigger,TContext}" /> does).
/// </remarks>
public static class StateMachineDocumentLoader
{
    /// <summary>Loads <paramref name="document" />.</summary>
    /// <param name="document">Document to load.</param>
    /// <param name="registry">Named guards, actions, selectors, and payload declarations.</param>
    /// <param name="stateConverter">Converts state keys; use <see cref="StringStateKeyConverter" /> for string states or <see cref="EnumStateKeyConverter{TEnum}" />.</param>
    /// <param name="triggerConverter">Converts trigger keys.</param>
    /// <param name="configure">Optional extra builder setup (clock, observers, <c>Strict</c>) applied before <c>Build</c>.</param>
    /// <exception cref="StateMachineConfigurationException">The document is invalid.</exception>
    public static StateMachineDefinition<TState, TTrigger, TContext> Load<TState, TTrigger, TContext>(
        StateMachineDocument document,
        StateMachineRegistry<TState, TTrigger, TContext> registry,
        IStateKeyConverter<TState> stateConverter,
        IStateKeyConverter<TTrigger> triggerConverter,
        Action<StateMachineBuilder<TState, TTrigger, TContext>>? configure = null)
        where TState : notnull where TTrigger : notnull
    {
        ArgumentHelpers.ThrowIfNull(document);
        ArgumentHelpers.ThrowIfNull(registry);
        ArgumentHelpers.ThrowIfNull(stateConverter);
        ArgumentHelpers.ThrowIfNull(triggerConverter);
        var errors = new List<string>();
        var states = new Dictionary<string, TState>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(document.Name))
            errors.Add("Name is required.");

        foreach (var stateDocument in document.States) {
            if (states.ContainsKey(stateDocument.Name))
                errors.Add($"State '{stateDocument.Name}' is declared more than once.");
            else if (Convert(stateConverter, stateDocument.Name, "state", errors, out var state))
                states[stateDocument.Name] = state;
        }

        var steps = new List<Action<StateMachineBuilder<TState, TTrigger, TContext>>>();
        var initial = State(document.InitialState, "initial state");
        foreach (var state in states.Values)
            steps.Add(b => b.Configure(state));

        foreach (var name in document.OnTransition) {
            if (Hook(name, "transition hook") is { } hook)
                steps.Add(b => b.AddTransitionHook(hook));
        }

        foreach (var stateDocument in document.States)
            AddState(stateDocument);

        if (errors.Count > 0)
            throw new StateMachineConfigurationException($"State machine document '{document.Name}' is invalid:\n- {string.Join("\n- ", errors)}");

        var builder = new StateMachineBuilder<TState, TTrigger, TContext>(document.Name, initial);
        foreach (var step in steps)
            step(builder);

        configure?.Invoke(builder);
        return builder.Build();

        void AddState(StateDocument stateDocument)
        {
            if (!states.TryGetValue(stateDocument.Name, out var state))
                return;

            var where = $"state '{stateDocument.Name}'";
            if (stateDocument.Parent is not null) {
                var parent = State(stateDocument.Parent, $"parent of {where}");
                steps.Add(b => b.Configure(state).SubstateOf(parent));
            }

            if (stateDocument.InitialSubstate is not null) {
                var child = State(stateDocument.InitialSubstate, $"initial substate of {where}");
                steps.Add(b => b.Configure(state).InitialSubstate(child));
            }

            if (stateDocument.IsFinal)
                steps.Add(b => b.Configure(state).AsFinal());

            foreach (var name in stateDocument.OnEntry) {
                if (Hook(name, $"entry hook of {where}") is { } hook)
                    steps.Add(b => b.Configure(state).AddEntry(hook));
            }

            foreach (var name in stateDocument.OnExit) {
                if (Hook(name, $"exit hook of {where}") is { } hook)
                    steps.Add(b => b.Configure(state).AddExit(hook));
            }

            foreach (var timer in stateDocument.Timers) {
                if (Convert(triggerConverter, timer.Trigger, $"trigger in timer of {where}", errors, out var trigger))
                    steps.Add(b => b.Configure(state).After(timer.After, trigger));
            }

            foreach (var transition in stateDocument.Transitions) {
                if (Rule(transition, state, where) is { } rule)
                    steps.Add(b => b.Configure(state).AddRule(rule));
            }
        }

        TransitionRule<TState, TTrigger, TContext>? Rule(TransitionDocument transition, TState source, string where)
        {
            if (!Convert(triggerConverter, transition.Trigger, $"trigger in {where}", errors, out var trigger))
                return null;

            where = $"'{transition.Trigger}' rule of {where}";
            var count = errors.Count;
            registry.Payloads.TryGetValue(trigger, out var payloadType);
            Func<TContext, object?, bool>? guard = null;
            var guardNeedsPayload = false;
            if (transition.Guard is not null) {
                if (!registry.Guards.TryGetValue(transition.Guard, out var registered))
                    errors.Add($"Guard '{transition.Guard}' ({where}) is not registered.");
                else {
                    CheckPayload(registered.PayloadType, payloadType, $"Guard '{transition.Guard}'", where);
                    guard = registered.Guard;
                    guardNeedsPayload = registered.PayloadType is not null;
                }
            }

            var rule = new TransitionRule<TState, TTrigger, TContext> {
                Trigger = trigger,
                Kind = transition.Kind switch {
                    TransitionDocumentKind.Permit => RuleKind.Permit,
                    TransitionDocumentKind.Reentry => RuleKind.Reentry,
                    TransitionDocumentKind.Internal => RuleKind.Internal,
                    TransitionDocumentKind.Ignore => RuleKind.Ignore,
                    var _ => RuleKind.Dynamic
                },
                Destination = transition.Kind switch {
                    TransitionDocumentKind.Permit => State(transition.Destination, $"destination of {where}"),
                    TransitionDocumentKind.Reentry => source,
                    var _ => default!
                },
                Destinations = transition.Kind == TransitionDocumentKind.Dynamic ? transition.Destinations.Select(d => State(d, $"destination of {where}")).ToArray() : [],
                Guard = guard,
                GuardNeedsPayload = guardNeedsPayload,
                GuardDescription = transition.Guard,
                PayloadType = payloadType,
                Action = transition.Kind == TransitionDocumentKind.Internal ? Hook(transition.Action, $"action of {where}") : null,
                Selector = transition.Kind == TransitionDocumentKind.Dynamic ? Selector(transition.Selector, payloadType, where) : null,
                SelectorName = transition.Selector
            };

            return errors.Count == count ? rule : null;
        }

        Func<TContext, object?, TState>? Selector(string? name, Type? payloadType, string where)
        {
            if (name is null) {
                errors.Add($"Selector is required ({where}).");
                return null;
            }

            if (!registry.Selectors.TryGetValue(name, out var registered)) {
                errors.Add($"Selector '{name}' ({where}) is not registered.");
                return null;
            }

            CheckPayload(registered.PayloadType, payloadType, $"Selector '{name}'", where);
            return registered.Selector;
        }

        void CheckPayload(Type? expected, Type? declared, string what, string where)
        {
            if (expected is not null && expected != declared)
                errors.Add($"{what} ({where}) takes a {expected.Name} payload, but the trigger is declared with {declared?.Name ?? "no payload"}.");
        }

        Hook<TState, TTrigger, TContext>? Hook(string? name, string where)
        {
            if (name is null) {
                errors.Add($"Action is required ({where}).");
                return null;
            }

            if (registry.Actions.TryGetValue(name, out var hook))
                return hook;

            errors.Add($"Action '{name}' ({where}) is not registered.");
            return null;
        }

        TState State(string? key, string where)
        {
            if (key is not null && states.TryGetValue(key, out var state))
                return state;

            errors.Add($"State '{key}' ({where}) is not declared in the document.");
            return default!;
        }
    }

    private static bool Convert<TValue>(IStateKeyConverter<TValue> converter, string key, string what, List<string> errors, out TValue value)
        where TValue : notnull
    {
        try {
            value = converter.FromKey(key);
            return true;
        }
        catch (ArgumentException ex) {
            errors.Add($"Unknown {what} '{key}': {ex.Message}");
            value = default!;
            return false;
        }
    }
}