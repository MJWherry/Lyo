namespace Lyo.Algorithms;

/// <summary>Frozen startup algorithms (named C# and options expressions). Reads are lock-free after <c>AddAlgorithms</c>.</summary>
public interface IAlgorithmStartupStore
{
    /// <summary>True when any startup algorithm uses <paramref name="key" />, ignoring context and result types.</summary>
    bool ContainsKey(string key);

    /// <summary>Returns the startup algorithm for this key and type pair, or false when it was not registered.</summary>
    bool TryGet<TContext, TResult>(string key, out IAlgorithm<TContext, TResult>? algorithm);
}
