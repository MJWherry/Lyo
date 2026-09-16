using Lyo.Exceptions;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>Process-local <see cref="IStateMachineDefinitionStore" /> with the same revision checks as a database store. For tests and single-process hosts.</summary>
public sealed class InMemoryStateMachineDefinitionStore : IStateMachineDefinitionStore
{
    private readonly Dictionary<string, List<StateMachineDocument>> _revisions = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    /// <inheritdoc />
    public Task<bool> SaveAsync(StateMachineDocument document, int expectedRevision, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(document);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(document.Name);
        lock (_gate) {
            if (!_revisions.TryGetValue(document.Name, out var revisions))
                _revisions[document.Name] = revisions = [];

            if (revisions.Count != expectedRevision)
                return Task.FromResult(false);

            revisions.Add(document with { Revision = expectedRevision + 1 });
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<StateMachineDocument?> GetAsync(string name, int? revision = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        lock (_gate) {
            if (!_revisions.TryGetValue(name, out var revisions) || revisions.Count == 0)
                return Task.FromResult<StateMachineDocument?>(null);

            var index = (revision ?? revisions.Count) - 1;
            return Task.FromResult(index >= 0 && index < revisions.Count ? revisions[index] : null);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListAsync(CancellationToken ct = default)
    {
        lock (_gate) {
            IReadOnlyList<string> names = _revisions.Where(p => p.Value.Count > 0).Select(p => p.Key).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            return Task.FromResult(names);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> ListRevisionsAsync(string name, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        lock (_gate) {
            IReadOnlyList<int> revisions = _revisions.TryGetValue(name, out var list) ? list.Select(d => d.Revision).ToArray() : [];
            return Task.FromResult(revisions);
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string name, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        lock (_gate)
            return Task.FromResult(_revisions.Remove(name));
    }
}
