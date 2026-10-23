using Lyo.Exceptions;

namespace Lyo.Algorithms;

/// <summary>Startup store first, then the runtime catalog. A cache hit returns the value without an async state machine.</summary>
public sealed class AlgorithmRegistry : IAlgorithmRegistry
{
    private readonly AlgorithmOptions _options;
    private readonly IAlgorithmRuntimeStore _runtime;
    private readonly IAlgorithmStartupStore _startup;

    /// <summary>Creates a registry over the frozen startup store and the catalog runtime store.</summary>
    public AlgorithmRegistry(IAlgorithmStartupStore startup, IAlgorithmRuntimeStore runtime, AlgorithmOptions options)
    {
        ArgumentHelpers.ThrowIfNull(startup);
        ArgumentHelpers.ThrowIfNull(runtime);
        ArgumentHelpers.ThrowIfNull(options);
        _startup = startup;
        _runtime = runtime;
        _options = options;
    }

    /// <inheritdoc />
    public TResult Execute<TContext, TResult>(string key, TContext context)
    {
        AlgorithmKeys.ThrowIfInvalidKey(key);
        return Execute<TContext, TResult>(AlgorithmRef.Latest(key), context);
    }

    /// <inheritdoc />
    public TResult Execute<TContext, TResult>(AlgorithmRef reference, TContext context)
    {
        ArgumentHelpers.ThrowIfNull(reference);
        ArgumentHelpers.ThrowIfNull(context);
        if (TryStartup(reference, context, out TResult value) || _runtime.TryExecute(reference, context, out value))
            return value;

        return AlgorithmHelpers.Throw<TResult>($"Algorithm '{reference.Key}' is not compiled. Call ExecuteAsync to load it.");
    }

    /// <inheritdoc />
    public Task<TResult> ExecuteAsync<TContext, TResult>(string key, TContext context, CancellationToken ct = default)
    {
        AlgorithmKeys.ThrowIfInvalidKey(key);
        return ExecuteAsync<TContext, TResult>(AlgorithmRef.Latest(key), context, ct);
    }

    /// <inheritdoc />
    public Task<TResult> ExecuteAsync<TContext, TResult>(AlgorithmRef reference, TContext context, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(reference);
        ArgumentHelpers.ThrowIfNull(context);
        if (TryStartup(reference, context, out TResult value) || _runtime.TryExecute(reference, context, out value))
            return Task.FromResult(value);

        return LoadAsync<TContext, TResult>(reference, context, ct);
    }

    /// <inheritdoc />
    public async Task<AlgorithmExecution<TResult>> ExecuteWithBreakdownAsync<TContext, TResult>(AlgorithmRef reference, TContext context, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(reference);
        ArgumentHelpers.ThrowIfNull(context);
        if (TryStartup(reference, context, out TResult startupValue))
            return new() { Value = startupValue, Steps = [] };

        var algorithm = await _runtime.ResolveAsync<TContext, TResult>(reference, ct).ConfigureAwait(false);
        if (algorithm is null) {
            if (reference.Address == AlgorithmAddress.Latest && _startup.TryGet<TContext, TResult>(reference.Key, out var startup) && startup is not null)
                return new() { Value = startup.Execute(context), Steps = [] };

            return AlgorithmHelpers.Throw<AlgorithmExecution<TResult>>($"Algorithm '{reference.Key}' was not found.");
        }

        var steps = AlgorithmTrace.Begin();
        try {
            return new() { Value = algorithm.Execute(context), Steps = steps };
        }
        finally {
            AlgorithmTrace.End();
        }
    }

    private async Task<TResult> LoadAsync<TContext, TResult>(AlgorithmRef reference, TContext context, CancellationToken ct)
    {
        var runtime = await _runtime.ResolveAsync<TContext, TResult>(reference, ct).ConfigureAwait(false);
        if (runtime is not null)
            return runtime.Execute(context);

        if (reference.Address == AlgorithmAddress.Latest && _startup.TryGet<TContext, TResult>(reference.Key, out var startup) && startup is not null)
            return startup.Execute(context);

        return AlgorithmHelpers.Throw<TResult>($"Algorithm '{reference.Key}' was not found.");
    }

    private bool TryStartup<TContext, TResult>(AlgorithmRef reference, TContext context, out TResult result)
    {
        result = default!;
        if (reference.Address != AlgorithmAddress.Latest || _options.AllowRuntimeOverride)
            return false;

        if (!_startup.TryGet<TContext, TResult>(reference.Key, out var algorithm) || algorithm is null)
            return false;

        result = algorithm.Execute(context);
        return true;
    }
}