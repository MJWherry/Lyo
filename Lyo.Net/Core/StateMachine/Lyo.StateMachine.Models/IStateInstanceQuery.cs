namespace Lyo.StateMachine.Models;

/// <summary>One saved instance returned by <see cref="IStateInstanceQuery{TState}" />.</summary>
/// <param name="InstanceKey">Key of the instance within its machine.</param>
/// <param name="Snapshot">Saved snapshot.</param>
/// <param name="UpdatedAtUtc">When the store last wrote the instance.</param>
public sealed record StateInstance<TState>(string InstanceKey, StateSnapshot<TState> Snapshot, DateTime UpdatedAtUtc)
    where TState : notnull;

/// <summary>Filters for <see cref="IStateInstanceQuery{TState}.ListAsync" />. Unset members do not filter.</summary>
/// <remarks>
/// Stores do not know the state hierarchy. To include substates, pass the parent and its descendants in <see cref="States" /> (see
/// <c>StateMachineDefinition.GetSelfAndDescendants</c>).
/// </remarks>
public sealed class StateInstanceFilter<TState>
    where TState : notnull
{
    /// <summary>Only instances whose current state is in this set.</summary>
    public IReadOnlyCollection<TState>? States { get; init; }

    /// <summary>Only instances with a timer due at or before this UTC time. Results are then ordered by due time.</summary>
    public DateTime? DueBeforeUtc { get; init; }

    /// <summary>Only instances written after this UTC time.</summary>
    public DateTime? UpdatedAfterUtc { get; init; }
}

/// <summary>Read and delete access to saved instances and their transition history. Implemented next to <see cref="IStateStore{TState}" /> by each store.</summary>
public interface IStateInstanceQuery<TState>
    where TState : notnull
{
    /// <summary>Lists instances of a machine, ordered by due time when <see cref="StateInstanceFilter{TState}.DueBeforeUtc" /> is set, otherwise by instance key.</summary>
    Task<IReadOnlyList<StateInstance<TState>>> ListAsync(
        string machineName,
        StateInstanceFilter<TState>? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken ct = default);

    /// <summary>Number of instances in each state. States with no instances are omitted.</summary>
    Task<IReadOnlyDictionary<TState, int>> CountByStateAsync(string machineName, CancellationToken ct = default);

    /// <summary>Transition history for an instance, oldest first.</summary>
    Task<IReadOnlyList<TransitionRecord<TState>>> GetHistoryAsync(string machineName, string instanceKey, int skip = 0, int take = 100, CancellationToken ct = default);

    /// <summary>Deletes an instance and its history.</summary>
    /// <param name="machineName">Machine name.</param>
    /// <param name="instanceKey">Instance key.</param>
    /// <param name="expectedVersion">When set, delete only if the stored version matches.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>False when the instance does not exist or the version did not match.</returns>
    Task<bool> DeleteAsync(string machineName, string instanceKey, long? expectedVersion = null, CancellationToken ct = default);
}