using System.Collections.Concurrent;
using Lyo.Authentication.Exceptions;
using Lyo.Authentication.Models.Scopes;
using Lyo.Common.Extensions;
using Lyo.Exceptions;

namespace Lyo.Authentication.Scopes;

/// <summary>Default in-memory <see cref="IScopeRegistry" />. Thread-safe for concurrent registration during startup.</summary>
public sealed class ScopeRegistry : IScopeRegistry
{
    private readonly List<string> _order = [];
    private readonly object _orderLock = new();
    private readonly ConcurrentDictionary<string, Scope> _scopes = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyList<Scope> All {
        get {
            lock (_orderLock)
                return _order.Select(n => _scopes[n]).ToArray();
        }
    }

    /// <inheritdoc />
    public Scope? TryGet(string name) => _scopes.TryGetValue(name, out var s) ? s : null;

    /// <inheritdoc />
    public bool IsRegistered(string name) => _scopes.ContainsKey(name);

    /// <inheritdoc />
    public IReadOnlyCollection<string> Expand(IEnumerable<string> names)
    {
        ArgumentHelpers.ThrowIfNull(names);
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in names) {
            if (n.IsNullOrEmpty())
                continue;

            if (!_scopes.TryGetValue(n, out var scope))
                throw new ScopeNotRegisteredException(n);

            foreach (var t in scope.TransitiveImplies)
                result.Add(t);
        }

        return result;
    }

    /// <summary>Registers a scope. Re-registration overwrites the description / implies but preserves ordering.</summary>
    public void Register(string name, string description, params string[] implies)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNull(description);
        ArgumentHelpers.ThrowIfNull(implies);
        ValidateName(name);
        var transitiveImplies = ComputeTransitiveImplies(name, implies);
        var scope = new Scope(name, description, implies.ToArray(), transitiveImplies);
        _scopes[name] = scope;
        lock (_orderLock) {
            if (!_order.Contains(name))
                _order.Add(name);
        }
    }

    private IReadOnlyCollection<string> ComputeTransitiveImplies(string name, IReadOnlyList<string> direct)
    {
        var result = new HashSet<string>(StringComparer.Ordinal) { name };
        var stack = new Stack<string>(direct);
        while (stack.Count > 0) {
            var next = stack.Pop();
            if (!result.Add(next))
                continue;

            if (_scopes.TryGetValue(next, out var existing)) {
                foreach (var t in existing.Implies)
                    stack.Push(t);
            }
        }

        return result;
    }

    private static void ValidateName(string name)
    {
        foreach (var c in name) {
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.' || c == ':' || c == '_' || c == '-')
                continue;

            throw new ArgumentException($"Scope name '{name}' contains invalid character '{c}'. Allowed: lowercase ASCII letters, digits, '.', ':', '_', '-'.", nameof(name));
        }
    }
}