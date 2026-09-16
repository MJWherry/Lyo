namespace Lyo.StateMachine.Models;

/// <summary>The saved state of one state machine instance (a job row, a character, a worker).</summary>
/// <remarks>
/// Snapshots are immutable. A successful transition returns a new snapshot with <see cref="Version" /> incremented by one, so stores can use the version for optimistic
/// concurrency. A brand-new instance starts at version 0. All times are UTC; the engine and stores reject other kinds.
/// </remarks>
/// <typeparam name="TState">State type, usually an enum.</typeparam>
/// <param name="State">Current (innermost) state.</param>
/// <param name="Version">Number of successful state changes (and data updates) applied to this instance.</param>
/// <param name="EnteredAtUtc">When <paramref name="State" /> was entered, in UTC.</param>
/// <param name="DueAtUtc">When the earliest timer on <paramref name="State" /> fires, or null when the state has no timer. Stores index it so workers can find due instances.</param>
/// <param name="DataJson">Optional JSON document stored with the instance. Transitions carry it forward unchanged.</param>
public sealed record StateSnapshot<TState>(TState State, long Version, DateTime EnteredAtUtc, DateTime? DueAtUtc = null, string? DataJson = null)
    where TState : notnull;
