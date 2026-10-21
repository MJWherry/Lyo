namespace Lyo.StateMachine.Models;

/// <summary>A state machine definition as data: states, transitions, and the names of guards, hooks, and selectors to resolve from a registry at load time.</summary>
/// <remarks>
/// States and triggers are strings (converted with <see cref="IStateKeyConverter{TValue}" />). Code cannot be stored, so every guard, hook, action, and dynamic selector is
/// referenced by name. <see cref="Revision" /> is assigned by <see cref="IStateMachineDefinitionStore" /> when saved.
/// </remarks>
public sealed record StateMachineDocument
{
    /// <summary>Machine name. Stores key saved instances by it.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Revision within the definition store, starting at 1. Zero for an unsaved document.</summary>
    public int Revision { get; init; }

    /// <summary>State new instances start in.</summary>
    public string InitialState { get; init; } = string.Empty;

    /// <summary>Named hooks run on every transition, after exit hooks and before entry hooks.</summary>
    public IReadOnlyList<string> OnTransition { get; init; } = [];

    /// <summary>States in declaration order. The initial state should be listed first.</summary>
    public IReadOnlyList<StateDocument> States { get; init; } = [];
}

/// <summary>One state in a <see cref="StateMachineDocument" />.</summary>
public sealed record StateDocument
{
    /// <summary>State key.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Parent state key, when this is a substate.</summary>
    public string? Parent { get; init; }

    /// <summary>Child entered automatically when this state is the destination.</summary>
    public string? InitialSubstate { get; init; }

    /// <summary>True for terminal states, which are allowed to have no way out.</summary>
    public bool IsFinal { get; init; }

    /// <summary>Named entry hooks.</summary>
    public IReadOnlyList<string> OnEntry { get; init; } = [];

    /// <summary>Named exit hooks.</summary>
    public IReadOnlyList<string> OnExit { get; init; } = [];

    /// <summary>Timers that fire a trigger after the state has been current for a while.</summary>
    public IReadOnlyList<TimerDocument> Timers { get; init; } = [];

    /// <summary>Outgoing rules in priority order.</summary>
    public IReadOnlyList<TransitionDocument> Transitions { get; init; } = [];
}

/// <summary>A timer on a state.</summary>
public sealed record TimerDocument
{
    /// <summary>Time in the state before the trigger is due.</summary>
    public TimeSpan After { get; init; }

    /// <summary>Trigger key fired when due. It must be permitted from the state and take no payload.</summary>
    public string Trigger { get; init; } = string.Empty;
}

/// <summary>Kinds of rule in a <see cref="TransitionDocument" />.</summary>
public enum TransitionDocumentKind
{
    /// <summary>Move to <see cref="TransitionDocument.Destination" />.</summary>
    Permit,

    /// <summary>Exit and re-enter the state.</summary>
    Reentry,

    /// <summary>Run <see cref="TransitionDocument.Action" /> without exit or entry.</summary>
    Internal,

    /// <summary>Accept the trigger and do nothing.</summary>
    Ignore,

    /// <summary>Move to the state chosen by <see cref="TransitionDocument.Selector" />, one of <see cref="TransitionDocument.Destinations" />.</summary>
    Dynamic
}

/// <summary>One rule in a <see cref="StateDocument" />.</summary>
public sealed record TransitionDocument
{
    /// <summary>Rule kind.</summary>
    public TransitionDocumentKind Kind { get; init; }

    /// <summary>Trigger key.</summary>
    public string Trigger { get; init; } = string.Empty;

    /// <summary>Destination state key for <see cref="TransitionDocumentKind.Permit" />.</summary>
    public string? Destination { get; init; }

    /// <summary>Possible destination state keys for <see cref="TransitionDocumentKind.Dynamic" />.</summary>
    public IReadOnlyList<string> Destinations { get; init; } = [];

    /// <summary>Registry name of the guard, when the rule is guarded.</summary>
    public string? Guard { get; init; }

    /// <summary>Registry name of the action for <see cref="TransitionDocumentKind.Internal" />.</summary>
    public string? Action { get; init; }

    /// <summary>Registry name of the selector for <see cref="TransitionDocumentKind.Dynamic" />.</summary>
    public string? Selector { get; init; }
}