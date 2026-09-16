using Lyo.Exceptions;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>One persisted entity driven by a shared definition: loads its snapshot from a store, fires, and saves with optimistic concurrency.</summary>
/// <remarks>
/// Hooks run before the save. When another writer saved first, the result is <see cref="TransitionRejection.ConcurrencyConflict" /> and nothing is written, but
/// hook side effects have already happened. With <see cref="StateMachineInstanceOptions.MaxConflictRetries" /> the instance reloads and fires again, which runs
/// the hooks again, so keep hooks idempotent. One instance allows one operation at a time; firing it again from inside its own hook throws
/// <see cref="StateMachineReentrancyException" />. Instances are cheap; create one per operation. Internal and ignored triggers are not saved.
/// </remarks>
public sealed class StateMachineInstance<TState, TTrigger, TContext>
    where TState : notnull where TTrigger : notnull
{
    private readonly StateMachineDefinition<TState, TTrigger, TContext> _definition;
    private readonly StateMachineInstanceOptions _options;
    private readonly IStateStore<TState> _store;
    private int _busy;

    /// <summary>Binds <paramref name="instanceKey" /> (for example a job id) to a definition and store.</summary>
    public StateMachineInstance(
        StateMachineDefinition<TState, TTrigger, TContext> definition,
        IStateStore<TState> store,
        string instanceKey,
        StateMachineInstanceOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(definition);
        ArgumentHelpers.ThrowIfNull(store);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(instanceKey);
        options ??= new();
        options.Validate();
        _definition = definition;
        _store = store;
        _options = options;
        InstanceKey = instanceKey;
    }

    /// <summary>Key of this instance within the machine.</summary>
    public string InstanceKey { get; }

    /// <summary>The saved snapshot, or the definition's initial snapshot when nothing has been saved yet.</summary>
    public async Task<StateSnapshot<TState>> GetSnapshotAsync(CancellationToken ct = default)
        => await _store.LoadAsync(_definition.Name, InstanceKey, ct).ConfigureAwait(false) ?? _definition.CreateInitialSnapshot();

    /// <summary>Loads, fires a trigger without payload, and saves on success.</summary>
    public async Task<TransitionResult<TState>> FireAsync(TContext context, TTrigger trigger, CancellationToken ct = default)
        => (await RunAsync(snapshot => new Attempt(_definition.FireAsync(context, snapshot, trigger, ct), trigger), ct).ConfigureAwait(false))!;

    /// <summary>Loads, fires a payload trigger, and saves on success.</summary>
    public async Task<TransitionResult<TState>> FireAsync<TPayload>(TContext context, Trigger<TTrigger, TPayload> trigger, TPayload payload, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(trigger);
        return (await RunAsync(snapshot => new Attempt(_definition.FireAsync(context, snapshot, trigger, payload, ct), trigger.Value), ct).ConfigureAwait(false))!;
    }

    /// <summary>Fires the current state's timer trigger when it is due at <paramref name="nowUtc" />. Returns null when nothing is due.</summary>
    /// <remarks>Call from the host's scheduler, typically for each result of <c>IStateInstanceQuery.ListAsync</c> with <c>DueBeforeUtc</c>.</remarks>
    public Task<TransitionResult<TState>?> FireDueAsync(TContext context, DateTime nowUtc, CancellationToken ct = default)
        => RunAsync(snapshot => _definition.GetDueTrigger(snapshot, nowUtc) is { } due ? new Attempt(_definition.FireAsync(context, snapshot, due.Trigger, ct), due.Trigger) : null, ct);

    /// <summary>Deserializes the instance's data, or returns default when none has been stored.</summary>
    public async Task<TData?> GetDataAsync<TData>(CancellationToken ct = default)
        => (await GetSnapshotAsync(ct).ConfigureAwait(false)).GetData<TState, TData>(_options.Json);

    /// <summary>Replaces the instance's data with the result of <paramref name="update" />. Bumps the version; no history row is written. State and timers are unchanged.</summary>
    /// <returns>False when another writer saved first and retries (if any) were exhausted.</returns>
    public async Task<bool> UpdateDataAsync<TData>(Func<TData?, TData> update, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(update);
        EnterBusy();
        try {
            for (var attempt = 0;; attempt++) {
                var current = await GetSnapshotAsync(ct).ConfigureAwait(false);
                var next = current.WithData(update(current.GetData<TState, TData>(_options.Json)), _options.Json) with { Version = current.Version + 1 };
                if (await _store.SaveAsync(_definition.Name, InstanceKey, next, current.Version, null, ct).ConfigureAwait(false))
                    return true;

                if (attempt >= _options.MaxConflictRetries)
                    return false;
            }
        }
        finally {
            Volatile.Write(ref _busy, 0);
        }
    }

    private async Task<TransitionResult<TState>?> RunAsync(Func<StateSnapshot<TState>, Attempt?> fire, CancellationToken ct)
    {
        EnterBusy();
        try {
            for (var attempt = 0;; attempt++) {
                var current = await GetSnapshotAsync(ct).ConfigureAwait(false);
                if (fire(current) is not { } next)
                    return null;

                var result = await next.Result.ConfigureAwait(false);
                if (!result.Changed)
                    return result;

                var record = new TransitionRecord<TState>(result.From, result.To, next.Trigger.ToString() ?? string.Empty, result.Snapshot.Version, result.Snapshot.EnteredAtUtc);
                if (await _store.SaveAsync(_definition.Name, InstanceKey, result.Snapshot, current.Version, record, ct).ConfigureAwait(false))
                    return result;

                if (attempt >= _options.MaxConflictRetries)
                    return new(TransitionRejection.ConcurrencyConflict, result.From, current);
            }
        }
        finally {
            Volatile.Write(ref _busy, 0);
        }
    }

    private void EnterBusy()
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1)
            throw new StateMachineReentrancyException($"State machine instance '{_definition.Name}/{InstanceKey}' is already running an operation.");
    }

    private readonly struct Attempt(Task<TransitionResult<TState>> result, TTrigger trigger)
    {
        public Task<TransitionResult<TState>> Result { get; } = result;

        public TTrigger Trigger { get; } = trigger;
    }
}
