namespace Lyo.StateMachine.Models;

/// <summary>What a fired trigger did.</summary>
public enum TransitionKind
{
    /// <summary>Nothing happened; see <see cref="TransitionResult{TState}.Rejection" />.</summary>
    None,

    /// <summary>The instance moved to another state and the version incremented.</summary>
    Transition,

    /// <summary>The instance exited and re-entered its state and the version incremented.</summary>
    Reentry,

    /// <summary>An internal action ran without exit or entry. The snapshot and version are unchanged.</summary>
    Internal,

    /// <summary>The trigger was accepted and ignored. The snapshot and version are unchanged.</summary>
    Ignored
}
