namespace Lyo.Algorithms;

/// <summary>Re-entrancy frame for <c>Algo</c> and <c>score</c>. Set only when a formula actually calls a child.</summary>
internal static class AlgorithmCallScope
{
    [ThreadStatic]
    private static Frame? _frame;

    public static bool HasFrame => _frame is not null;

    public static bool Begin(int maxDepth)
    {
        if (_frame is not null)
            return false;
        _frame = new Frame(maxDepth);
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

    public static double Call(string key)
    {
        var frame = AlgorithmHelpers.ThrowIfNull(_frame, "Algo is only available while an algorithm is executing.");
        var invoke = AlgorithmHelpers.ThrowIfNull(frame.Invoke, $"Algorithm '{key}' is not compiled.");
        return invoke(key);
    }

    public static Func<string, double>? Invoker => _frame?.Invoke;

    public static void SetInvoker(Func<string, double>? invoke)
    {
        if (_frame is not null)
            _frame.Invoke = invoke;
    }

    private sealed class Frame(int maxDepth)
    {
        public int MaxDepth { get; } = maxDepth;

        public int Depth { get; set; }

        public HashSet<string> Keys { get; } = new(StringComparer.Ordinal);

        public Func<string, double>? Invoke { get; set; }
    }
}
