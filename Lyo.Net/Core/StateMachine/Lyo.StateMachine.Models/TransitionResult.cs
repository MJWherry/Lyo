namespace Lyo.StateMachine.Models;

/// <summary>Outcome of firing a trigger. Invalid transitions are reported here rather than thrown.</summary>
/// <typeparam name="TState">State type, usually an enum.</typeparam>
/// <param name="Rejection"><see cref="TransitionRejection.None" /> on success, otherwise the reason nothing changed.</param>
/// <param name="From">State the trigger was fired from.</param>
/// <param name="Snapshot">
/// The new snapshot after a transition or re-entry. For internal, ignored, and rejected triggers, the snapshot the caller passed in, which it should keep
/// using.
/// </param>
/// <param name="Kind">What happened. <see cref="TransitionKind.None" /> for rejections.</param>
public sealed record TransitionResult<TState>(TransitionRejection Rejection, TState From, StateSnapshot<TState> Snapshot, TransitionKind Kind = TransitionKind.None)
    where TState : notnull
{
    /// <summary>True when the trigger was accepted (transition, re-entry, internal, or ignored).</summary>
    public bool Succeeded => Rejection == TransitionRejection.None;

    /// <summary>True when <see cref="Snapshot" /> is new and must be saved (transition or re-entry).</summary>
    public bool Changed => Kind is TransitionKind.Transition or TransitionKind.Reentry;

    /// <summary>State after the call. Equal to <see cref="From" /> unless the state changed.</summary>
    public TState To => Snapshot.State;
}
