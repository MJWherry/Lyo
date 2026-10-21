namespace Lyo.Algorithms;

/// <summary>Catalog-backed algorithms. After the first compile, execute reads an in-process slot and does not open the store.</summary>
public interface IAlgorithmRuntimeStore
{
    /// <summary>Invokes a delegate that is already compiled in this process. Returns false when the key still needs <see cref="ResolveAsync{TContext,TResult}" />.</summary>
    bool TryExecute<TContext, TResult>(AlgorithmRef reference, TContext context, out TResult result);

    /// <summary>Loads, compiles, and caches <paramref name="reference" />. Returns null when the catalog has no such key.</summary>
    Task<IAlgorithm<TContext, TResult>?> ResolveAsync<TContext, TResult>(AlgorithmRef reference, CancellationToken ct = default);
}