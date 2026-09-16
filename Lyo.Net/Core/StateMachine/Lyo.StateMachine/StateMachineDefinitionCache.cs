using System.Collections.Concurrent;
using Lyo.Exceptions;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>Loads definitions from an <see cref="IStateMachineDefinitionStore" /> and caches each (name, revision) once. Thread-safe; register as a singleton.</summary>
/// <remarks>
/// Asking for the latest revision reads the store every call (a cheap lookup) so newly published revisions are picked up, but each revision is only loaded
/// once. Revisions are immutable, so entries never go stale; <see cref="Invalidate" /> only frees memory.
/// </remarks>
public sealed class StateMachineDefinitionCache<TState, TTrigger, TContext>
    where TState : notnull where TTrigger : notnull
{
    private readonly Action<StateMachineBuilder<TState, TTrigger, TContext>>? _configure;
    private readonly ConcurrentDictionary<(string Name, int Revision), Lazy<StateMachineDefinition<TState, TTrigger, TContext>>> _entries = new();
    private readonly StateMachineRegistry<TState, TTrigger, TContext> _registry;
    private readonly IStateKeyConverter<TState> _stateConverter;
    private readonly IStateMachineDefinitionStore _store;
    private readonly IStateKeyConverter<TTrigger> _triggerConverter;

    /// <summary>Creates the cache. <paramref name="configure" /> is applied to every loaded builder (clock, observers).</summary>
    public StateMachineDefinitionCache(
        IStateMachineDefinitionStore store,
        StateMachineRegistry<TState, TTrigger, TContext> registry,
        IStateKeyConverter<TState> stateConverter,
        IStateKeyConverter<TTrigger> triggerConverter,
        Action<StateMachineBuilder<TState, TTrigger, TContext>>? configure = null)
    {
        ArgumentHelpers.ThrowIfNull(store);
        ArgumentHelpers.ThrowIfNull(registry);
        ArgumentHelpers.ThrowIfNull(stateConverter);
        ArgumentHelpers.ThrowIfNull(triggerConverter);
        _store = store;
        _registry = registry;
        _stateConverter = stateConverter;
        _triggerConverter = triggerConverter;
        _configure = configure;
    }

    /// <summary>Returns the definition for a revision, or the latest when <paramref name="revision" /> is null.</summary>
    /// <exception cref="KeyNotFoundException">The store has no such machine or revision.</exception>
    /// <exception cref="StateMachineConfigurationException">The stored document is invalid for the registry.</exception>
    public async Task<StateMachineDefinition<TState, TTrigger, TContext>> GetAsync(string name, int? revision = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        if (revision is { } pinned && _entries.TryGetValue((name, pinned), out var cached))
            return cached.Value;

        var document = await _store.GetAsync(name, revision, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"State machine '{name}'{(revision is null ? string.Empty : $" revision {revision}")} is not in the definition store.");

        var key = (name, document.Revision);
        var entry = _entries.GetOrAdd(key, _ => new(() => StateMachineDocumentLoader.Load(document, _registry, _stateConverter, _triggerConverter, _configure)));
        try {
            return entry.Value;
        }
        catch {
            _entries.TryRemove(key, out _);
            throw;
        }
    }

    /// <summary>Drops every cached revision of <paramref name="name" />.</summary>
    public void Invalidate(string name)
    {
        foreach (var key in _entries.Keys.Where(k => k.Name == name))
            _entries.TryRemove(key, out _);
    }
}
