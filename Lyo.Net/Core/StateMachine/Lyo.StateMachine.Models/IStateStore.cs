namespace Lyo.StateMachine.Models;

/// <summary>Loads and saves state machine snapshots keyed by machine name and instance key, with optimistic concurrency.</summary>
/// <remarks>
/// Implementations must make <see cref="SaveAsync" /> atomic: the snapshot (and optional history record) is written only when the stored version still equals
/// <c>expectedVersion</c>. Use <c>expectedVersion = 0</c> for an instance that has never been saved.
/// </remarks>
/// <typeparam name="TState">State type, usually an enum.</typeparam>
public interface IStateStore<TState>
    where TState : notnull
{
    /// <summary>Returns the saved snapshot, or null when the instance has never been saved.</summary>
    Task<StateSnapshot<TState>?> LoadAsync(string machineName, string instanceKey, CancellationToken ct = default);

    /// <summary>Saves <paramref name="snapshot" /> if the stored version equals <paramref name="expectedVersion" />.</summary>
    /// <returns>True when saved. False when another writer got there first; nothing is written.</returns>
    Task<bool> SaveAsync(
        string machineName,
        string instanceKey,
        StateSnapshot<TState> snapshot,
        long expectedVersion,
        TransitionRecord<TState>? transition = null,
        CancellationToken ct = default);
}