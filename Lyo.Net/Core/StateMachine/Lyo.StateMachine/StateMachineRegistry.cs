using Lyo.Exceptions;
using Lyo.StateMachine.Internal;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>
/// Named code for data-driven machines: guards, actions (hooks), dynamic selectors, and payload declarations that a <see cref="StateMachineDocument" /> references by
/// name. Pass it to <see cref="StateMachineDocumentLoader" /> or <see cref="StateMachineDefinitionCache{TState,TTrigger,TContext}" />.
/// </summary>
/// <remarks>
/// Register everything once at startup. Names are case-sensitive and unique per kind. A payload-typed guard or selector can only be used on a trigger declared with
/// <see cref="Payload{TPayload}" /> for the same type.
/// </remarks>
/// <example>
/// <code>
/// var registry = new StateMachineRegistry&lt;string, string, JobContext&gt;()
///     .Guard("can fail", c => c.CanFail)
///     .Payload&lt;RetryPayload&gt;("retry")
///     .Guard&lt;RetryPayload&gt;("attempts left", (c, p) => p.Attempt &lt; c.MaxAttempts)
///     .Action("notify", t => t.Context.Notify(t.Destination));
/// </code>
/// </example>
public sealed class StateMachineRegistry<TState, TTrigger, TContext>
    where TState : notnull where TTrigger : notnull
{
    internal Dictionary<string, Hook<TState, TTrigger, TContext>> Actions { get; } = new(StringComparer.Ordinal);

    internal Dictionary<string, (Func<TContext, object?, bool> Guard, Type? PayloadType)> Guards { get; } = new(StringComparer.Ordinal);

    internal Dictionary<TTrigger, Type> Payloads { get; } = new();

    internal Dictionary<string, (Func<TContext, object?, TState> Selector, Type? PayloadType)> Selectors { get; } = new(StringComparer.Ordinal);

    /// <summary>Registers a guard on the context.</summary>
    /// <exception cref="StateMachineConfigurationException">A guard with this name is already registered.</exception>
    public StateMachineRegistry<TState, TTrigger, TContext> Guard(string name, Func<TContext, bool> guard)
    {
        ArgumentHelpers.ThrowIfNull(guard);
        return Add(Guards, name, ((c, _) => guard(c), null), "guard");
    }

    /// <summary>Registers a guard on the context and a typed payload. Use only on triggers declared with <see cref="Payload{TPayload}" /> for <typeparamref name="TPayload" />.</summary>
    /// <exception cref="StateMachineConfigurationException">A guard with this name is already registered.</exception>
    public StateMachineRegistry<TState, TTrigger, TContext> Guard<TPayload>(string name, Func<TContext, TPayload, bool> guard)
    {
        ArgumentHelpers.ThrowIfNull(guard);
        return Add(Guards, name, ((c, p) => guard(c, (TPayload)p!), typeof(TPayload)), "guard");
    }

    /// <summary>Registers an action usable as an entry, exit, transition, or internal-transition hook.</summary>
    /// <exception cref="StateMachineConfigurationException">An action with this name is already registered.</exception>
    public StateMachineRegistry<TState, TTrigger, TContext> Action(string name, Action<TransitionInfo<TState, TTrigger, TContext>> action)
    {
        ArgumentHelpers.ThrowIfNull(action);
        return Add(Actions, name, new(action, null, name), "action");
    }

    /// <summary>Async form of <see cref="Action" />. Machines that use it can only be fired with <c>FireAsync</c>.</summary>
    /// <exception cref="StateMachineConfigurationException">An action with this name is already registered.</exception>
    public StateMachineRegistry<TState, TTrigger, TContext> ActionAsync(string name, Func<TransitionInfo<TState, TTrigger, TContext>, CancellationToken, Task> action)
    {
        ArgumentHelpers.ThrowIfNull(action);
        return Add(Actions, name, new(null, action, name), "action");
    }

    /// <summary>Registers a dynamic-destination selector on the context. It must return one of the rule's declared destinations.</summary>
    /// <exception cref="StateMachineConfigurationException">A selector with this name is already registered.</exception>
    public StateMachineRegistry<TState, TTrigger, TContext> Selector(string name, Func<TContext, TState> selector)
    {
        ArgumentHelpers.ThrowIfNull(selector);
        return Add(Selectors, name, ((c, _) => selector(c), null), "selector");
    }

    /// <summary>Registers a dynamic-destination selector on the context and a typed payload.</summary>
    /// <exception cref="StateMachineConfigurationException">A selector with this name is already registered.</exception>
    public StateMachineRegistry<TState, TTrigger, TContext> Selector<TPayload>(string name, Func<TContext, TPayload, TState> selector)
    {
        ArgumentHelpers.ThrowIfNull(selector);
        return Add(Selectors, name, ((c, p) => selector(c, (TPayload)p!), typeof(TPayload)), "selector");
    }

    /// <summary>Declares that <paramref name="trigger" /> carries a <typeparamref name="TPayload" />. Fire it with <c>new Trigger&lt;TTrigger, TPayload&gt;(trigger)</c>.</summary>
    public StateMachineRegistry<TState, TTrigger, TContext> Payload<TPayload>(TTrigger trigger)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        Payloads[trigger] = typeof(TPayload);
        return this;
    }

    private StateMachineRegistry<TState, TTrigger, TContext> Add<TValue>(Dictionary<string, TValue> map, string name, TValue value, string kind)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        if (map.ContainsKey(name))
            throw new StateMachineConfigurationException($"A {kind} named '{name}' is already registered.");

        map[name] = value;
        return this;
    }
}
