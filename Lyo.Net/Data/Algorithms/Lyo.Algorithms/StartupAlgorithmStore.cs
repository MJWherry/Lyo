namespace Lyo.Algorithms;

/// <summary>In-memory map of algorithms registered before the host finishes <c>AddAlgorithms</c>. Duplicate key and type pairs throw.</summary>
public sealed class StartupAlgorithmStore : IAlgorithmStartupStore
{
    private readonly Dictionary<StartupKey, object> _algorithms;

    /// <summary>Freezes <paramref name="registrations" />. Throws <see cref="AlgorithmException" /> on a duplicate key for the same context and result types.</summary>
    internal StartupAlgorithmStore(IEnumerable<IAlgorithmRegistration> registrations)
    {
        _algorithms = new();
        foreach (var registration in registrations) {
            var key = new StartupKey(registration.Key, registration.ContextType, registration.ResultType);
            AlgorithmHelpers.ThrowIf(_algorithms.ContainsKey(key), $"Startup algorithm '{registration.Key}' is already registered for {registration.ContextType.Name}, {registration.ResultType.Name}.");
            _algorithms.Add(key, registration.Algorithm);
        }
    }

    /// <summary>True when any startup algorithm uses <paramref name="key" />, ignoring context type. Used to block catalog shadowing.</summary>
    public bool ContainsKey(string key)
    {
        foreach (var entry in _algorithms) {
            if (string.Equals(entry.Key.Key, key, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool TryGet<TContext, TResult>(string key, out IAlgorithm<TContext, TResult>? algorithm)
    {
        if (_algorithms.TryGetValue(new(key, typeof(TContext), typeof(TResult)), out var found)) {
            algorithm = (IAlgorithm<TContext, TResult>)found;
            return true;
        }

        algorithm = null;
        return false;
    }

    private readonly record struct StartupKey(string Key, Type Context, Type Result);
}
