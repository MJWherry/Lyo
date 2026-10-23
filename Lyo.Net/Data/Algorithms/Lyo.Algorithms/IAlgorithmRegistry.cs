namespace Lyo.Algorithms;

/// <summary>Caller entry point. Startup algorithms win over the catalog unless <see cref="AlgorithmOptions.AllowRuntimeOverride" /> is set.</summary>
public interface IAlgorithmRegistry
{
    /// <summary>
    /// Runs a compiled algorithm. Startup keys are always ready. A catalog key must have been loaded with
    /// <see cref="ExecuteAsync{TContext,TResult}(string,TContext,CancellationToken)" /> in this process first.
    /// </summary>
    TResult Execute<TContext, TResult>(string key, TContext context);

    /// <summary>Runs the revision selected by <paramref name="reference" /> when that delegate is already compiled.</summary>
    TResult Execute<TContext, TResult>(AlgorithmRef reference, TContext context);

    /// <summary>Runs the latest body for <paramref name="key" />. Loads and compiles a cold catalog key, then invokes it.</summary>
    Task<TResult> ExecuteAsync<TContext, TResult>(string key, TContext context, CancellationToken ct = default);

    /// <summary>Runs the revision selected by <paramref name="reference" />. A cache hit does not await.</summary>
    Task<TResult> ExecuteAsync<TContext, TResult>(AlgorithmRef reference, TContext context, CancellationToken ct = default);

    /// <summary>
    /// Runs <paramref name="reference" /> and returns the value plus one step per scorer. The value-only <see cref="Execute{TContext,TResult}(AlgorithmRef,TContext)" /> path
    /// does not build steps.
    /// </summary>
    Task<AlgorithmExecution<TResult>> ExecuteWithBreakdownAsync<TContext, TResult>(AlgorithmRef reference, TContext context, CancellationToken ct = default);
}