namespace Lyo.StateMachine.Models;

/// <summary>Why a fired trigger did not change state. <see cref="None" /> means the transition succeeded.</summary>
public enum TransitionRejection
{
    /// <summary>The transition succeeded.</summary>
    None,

    /// <summary>The current state (and its ancestors) has no rule for the trigger.</summary>
    NoTransition,

    /// <summary>Rules exist for the trigger, but every guard returned false.</summary>
    GuardFailed,

    /// <summary>The instance was changed by someone else between load and save. Reload and fire again.</summary>
    ConcurrencyConflict
}