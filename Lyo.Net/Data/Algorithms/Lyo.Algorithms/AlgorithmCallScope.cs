namespace Lyo.Algorithms;

/// <summary>Invokes a child algorithm as the type the call site was compiled with.</summary>
internal interface IAlgorithmInvoker
{
    /// <summary>Runs <paramref name="key" /> and returns its value as <typeparamref name="T" />.</summary>
    T Invoke<T>(string key);
}

/// <summary>Re-entrancy frame for <c>Algo</c> and <c>score</c>. Set only when a formula actually calls a child.</summary>
internal static class AlgorithmCallScope
{
    [ThreadStatic]
    private static Frame? _frame;

    public static bool HasFrame => _frame is not null;

    public static IAlgorithmInvoker? Invoker => _frame?.Invoke;

    public static bool Begin(int maxDepth)
    {
        if (_frame is not null)
            return false;

        _frame = new(maxDepth);
        return true;
    }

    public static void End() => _frame = null;

    public static void Enter(string key)
    {
        var frame = AlgorithmHelpers.ThrowIfNull(_frame, "Algo is only available while an algorithm is executing.");
        AlgorithmHelpers.ThrowIf(!frame.Keys.Add(key), $"Algorithm '{key}' calls itself.");
        frame.Depth++;
        AlgorithmHelpers.ThrowIf(frame.Depth > frame.MaxDepth, $"Algorithm call chain exceeded the maximum depth of {frame.MaxDepth}.");
    }

    public static void Exit(string key)
    {
        if (_frame is null)
            return;

        _frame.Keys.Remove(key);
        _frame.Depth--;
    }

    /// <summary>Runs <paramref name="key" /> as <typeparamref name="T" />. The call site is closed over <typeparamref name="T" /> when the formula compiles.</summary>
    public static T Call<T>(string key)
    {
        var frame = AlgorithmHelpers.ThrowIfNull(_frame, "Algo is only available while an algorithm is executing.");
        var invoke = AlgorithmHelpers.ThrowIfNull(frame.Invoke, $"Algorithm '{key}' is not compiled.");
        return invoke.Invoke<T>(key);
    }

    public static void SetInvoker(IAlgorithmInvoker? invoke)
    {
        if (_frame is not null)
            _frame.Invoke = invoke;
    }

    private sealed class Frame(int maxDepth)
    {
        public int MaxDepth { get; } = maxDepth;

        public int Depth { get; set; }

        public HashSet<string> Keys { get; } = new(StringComparer.Ordinal);

        public IAlgorithmInvoker? Invoke { get; set; }
    }
}
