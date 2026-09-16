using Lyo.Exceptions;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>
/// Process-local <see cref="IStateStore{TState}" /> and <see cref="IStateInstanceQuery{TState}" /> with the same version checks as a database store. For games,
/// tests, and single-process workers.
/// </summary>
public sealed class InMemoryStateStore<TState> : IStateStore<TState>, IStateInstanceQuery<TState>
    where TState : notnull
{
    private readonly Dictionary<(string Machine, string Key), Entry> _entries = new();
    private readonly object _gate = new();

    /// <inheritdoc />
    public Task<StateSnapshot<TState>?> LoadAsync(string machineName, string instanceKey, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(instanceKey);
        lock (_gate)
            return Task.FromResult(_entries.TryGetValue((machineName, instanceKey), out var entry) ? entry.Snapshot : null);
    }

    /// <inheritdoc />
    public Task<bool> SaveAsync(
        string machineName,
        string instanceKey,
        StateSnapshot<TState> snapshot,
        long expectedVersion,
        TransitionRecord<TState>? transition = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(instanceKey);
        ArgumentHelpers.ThrowIfNull(snapshot);
        lock (_gate) {
            _entries.TryGetValue((machineName, instanceKey), out var entry);
            if ((entry?.Snapshot.Version ?? 0) != expectedVersion)
                return Task.FromResult(false);

            if (entry is null)
                _entries[(machineName, instanceKey)] = entry = new(snapshot);
            else
                entry.Snapshot = snapshot;

            entry.UpdatedAtUtc = DateTime.UtcNow;
            if (transition is not null)
                entry.History.Add(transition);

            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<StateInstance<TState>>> ListAsync(
        string machineName,
        StateInstanceFilter<TState>? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(machineName);
        lock (_gate) {
            var query = _entries.Where(p => p.Key.Machine == machineName).Select(p => new StateInstance<TState>(p.Key.Key, p.Value.Snapshot, p.Value.UpdatedAtUtc));
            if (filter?.States is { } states)
                query = query.Where(i => states.Contains(i.Snapshot.State));

            if (filter?.UpdatedAfterUtc is { } updatedAfter)
                query = query.Where(i => i.UpdatedAtUtc > updatedAfter);

            if (filter?.DueBeforeUtc is { } dueBefore) {
                query = query.Where(i => i.Snapshot.DueAtUtc <= dueBefore)
                    .OrderBy(i => i.Snapshot.DueAtUtc)
                    .ThenBy(i => i.InstanceKey, StringComparer.Ordinal);
            }
            else
                query = query.OrderBy(i => i.InstanceKey, StringComparer.Ordinal);

            IReadOnlyList<StateInstance<TState>> page = query.Skip(skip).Take(take).ToArray();
            return Task.FromResult(page);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<TState, int>> CountByStateAsync(string machineName, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(machineName);
        lock (_gate) {
            IReadOnlyDictionary<TState, int> counts = _entries.Where(p => p.Key.Machine == machineName).GroupBy(p => p.Value.Snapshot.State).ToDictionary(g => g.Key, g => g.Count());
            return Task.FromResult(counts);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TransitionRecord<TState>>> GetHistoryAsync(string machineName, string instanceKey, int skip = 0, int take = 100, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(instanceKey);
        IReadOnlyList<TransitionRecord<TState>> page = GetHistory(machineName, instanceKey).Skip(skip).Take(take).ToArray();
        return Task.FromResult(page);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string machineName, string instanceKey, long? expectedVersion = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(instanceKey);
        lock (_gate) {
            if (!_entries.TryGetValue((machineName, instanceKey), out var entry) || (expectedVersion is { } version && entry.Snapshot.Version != version))
                return Task.FromResult(false);

            return Task.FromResult(_entries.Remove((machineName, instanceKey)));
        }
    }

    /// <summary>Transition records saved for an instance, oldest first.</summary>
    public IReadOnlyList<TransitionRecord<TState>> GetHistory(string machineName, string instanceKey)
    {
        lock (_gate)
            return _entries.TryGetValue((machineName, instanceKey), out var entry) ? entry.History.ToArray() : [];
    }

    private sealed class Entry(StateSnapshot<TState> snapshot)
    {
        public StateSnapshot<TState> Snapshot { get; set; } = snapshot;

        public DateTime UpdatedAtUtc { get; set; }

        public List<TransitionRecord<TState>> History { get; } = [];
    }
}
