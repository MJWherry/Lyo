using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.StateMachine.Models;
using Lyo.StateMachine.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lyo.StateMachine.Postgres;

/// <summary>
/// PostgreSQL <see cref="IStateStore{TState}" /> and <see cref="IStateInstanceQuery{TState}" />. The version check, instance write, and history row commit in one
/// transaction.
/// </summary>
/// <remarks>
/// Timestamps are stored as <c>timestamp with time zone</c>, so snapshot and filter times must be UTC (<see cref="ArgumentException" /> otherwise) and round-trip at
/// microsecond precision. Data is stored as <c>jsonb</c>, which normalizes whitespace and key order. <c>ListAsync</c> key ordering follows the database collation.
/// </remarks>
public sealed class PostgresStateStore<TState> : IStateStore<TState>, IStateInstanceQuery<TState>
    where TState : notnull
{
    private readonly IDbContextFactory<StateMachineDbContext> _contextFactory;
    private readonly IStateKeyConverter<TState> _converter;

    /// <summary>Creates a store that opens a context per operation.</summary>
    public PostgresStateStore(IDbContextFactory<StateMachineDbContext> contextFactory, IStateKeyConverter<TState> converter)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        ArgumentHelpers.ThrowIfNull(converter);
        _contextFactory = contextFactory;
        _converter = converter;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StateInstance<TState>>> ListAsync(
        string machineName,
        StateInstanceFilter<TState>? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(machineName);
        EnsureUtc(filter?.DueBeforeUtc, nameof(filter));
        EnsureUtc(filter?.UpdatedAfterUtc, nameof(filter));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Instances.AsNoTracking().Where(i => i.MachineName == machineName);
        if (filter?.States is { } states) {
            var keys = states.Select(_converter.ToKey).ToList();
            query = query.Where(i => keys.Contains(i.State));
        }

        if (filter?.UpdatedAfterUtc is { } updatedAfter)
            query = query.Where(i => i.UpdatedTimestamp > updatedAfter);

        query = filter?.DueBeforeUtc is { } dueBefore
            ? query.Where(i => i.DueTimestamp != null && i.DueTimestamp <= dueBefore).OrderBy(i => i.DueTimestamp).ThenBy(i => i.InstanceKey)
            : query.OrderBy(i => i.InstanceKey);

        var rows = await query.Skip(skip).Take(take).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(r => new StateInstance<TState>(r.InstanceKey, ToSnapshot(r), r.UpdatedTimestamp)).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<TState, int>> CountByStateAsync(string machineName, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(machineName);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var counts = await context.Instances.Where(i => i.MachineName == machineName)
            .GroupBy(i => i.State)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return counts.ToDictionary(c => _converter.FromKey(c.State), c => c.Count);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransitionRecord<TState>>> GetHistoryAsync(string machineName, string instanceKey, int skip = 0, int take = 100, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(instanceKey);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Transitions.AsNoTracking()
            .Where(t => t.MachineName == machineName && t.InstanceKey == instanceKey)
            .OrderBy(t => t.Version)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.Select(r => new TransitionRecord<TState>(_converter.FromKey(r.FromState), _converter.FromKey(r.ToState), r.Trigger, r.Version, r.OccurredTimestamp)).ToArray();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string machineName, string instanceKey, long? expectedVersion = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(instanceKey);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var instance = context.Instances.Where(i => i.MachineName == machineName && i.InstanceKey == instanceKey);
        if (expectedVersion is { } version)
            instance = instance.Where(i => i.Version == version);

        if (await instance.ExecuteDeleteAsync(ct).ConfigureAwait(false) == 0)
            return false;

        await context.Transitions.Where(t => t.MachineName == machineName && t.InstanceKey == instanceKey).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<StateSnapshot<TState>?> LoadAsync(string machineName, string instanceKey, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(instanceKey);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var row = await context.Instances.AsNoTracking().FirstOrDefaultAsync(i => i.MachineName == machineName && i.InstanceKey == instanceKey, ct).ConfigureAwait(false);
        return row is null ? null : ToSnapshot(row);
    }

    /// <inheritdoc />
    public async Task<bool> SaveAsync(
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
        EnsureUtc(snapshot.EnteredAtUtc, nameof(snapshot));
        EnsureUtc(snapshot.DueAtUtc, nameof(snapshot));
        EnsureUtc(transition?.OccurredAtUtc, nameof(transition));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var state = _converter.ToKey(snapshot.State);
        var now = DateTime.UtcNow;
        if (expectedVersion == 0) {
            context.Instances.Add(
                new() {
                    MachineName = machineName,
                    InstanceKey = instanceKey,
                    State = state,
                    Version = snapshot.Version,
                    EnteredTimestamp = snapshot.EnteredAtUtc,
                    UpdatedTimestamp = now,
                    DueTimestamp = snapshot.DueAtUtc,
                    DataJson = snapshot.DataJson
                });
        }
        else {
            var updated = await context.Instances.Where(i => i.MachineName == machineName && i.InstanceKey == instanceKey && i.Version == expectedVersion)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(i => i.State, state)
                        .SetProperty(i => i.Version, snapshot.Version)
                        .SetProperty(i => i.EnteredTimestamp, snapshot.EnteredAtUtc)
                        .SetProperty(i => i.UpdatedTimestamp, now)
                        .SetProperty(i => i.DueTimestamp, snapshot.DueAtUtc)
                        .SetProperty(i => i.DataJson, snapshot.DataJson), ct)
                .ConfigureAwait(false);

            if (updated == 0)
                return false;
        }

        if (transition is not null)
            context.Transitions.Add(ToEntity(machineName, instanceKey, transition));

        try {
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) {
            return false;
        }

        await tx.CommitAsync(ct).ConfigureAwait(false);
        return true;
    }

    private static void EnsureUtc(DateTime? value, string paramName)
    {
        if (value is { Kind: not DateTimeKind.Utc })
            throw new ArgumentException("Times must be UTC.", paramName);
    }

    private StateSnapshot<TState> ToSnapshot(StateMachineInstanceEntity row)
        => new(_converter.FromKey(row.State), row.Version, row.EnteredTimestamp, row.DueTimestamp, row.DataJson);

    private StateMachineTransitionEntity ToEntity(string machineName, string instanceKey, TransitionRecord<TState> transition)
        => new() {
            Id = LyoGuid.CreateV7(),
            MachineName = machineName,
            InstanceKey = instanceKey,
            FromState = _converter.ToKey(transition.From),
            ToState = _converter.ToKey(transition.To),
            Trigger = transition.Trigger,
            Version = transition.Version,
            OccurredTimestamp = transition.OccurredAtUtc
        };
}