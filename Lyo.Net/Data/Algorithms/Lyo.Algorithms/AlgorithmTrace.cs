namespace Lyo.Algorithms;

/// <summary>Optional step list for breakdown. The value-only path leaves this unset and allocates nothing.</summary>
internal static class AlgorithmTrace
{
    [ThreadStatic]
    private static Stack<List<AlgorithmStep>>? _stack;

    public static bool IsActive => _stack is not null;

    public static List<AlgorithmStep> Begin()
    {
        var root = new List<AlgorithmStep>();
        _stack = new();
        _stack.Push(root);
        return root;
    }

    public static void End() => _stack = null;

    public static T Run<T>(string key, AlgorithmKind kind, string? input, Func<T> execute, Func<T, double> points)
    {
        if (_stack is null)
            return execute();

        var children = new List<AlgorithmStep>();
        _stack.Push(children);
        T value;
        try {
            value = execute();
        }
        finally {
            _stack.Pop();
        }

        _stack.Peek()
        .Add(
            new() {
                Key = key,
                Kind = kind,
                Input = input,
                Points = points(value),
                Children = children
            });

        return value;
    }
}