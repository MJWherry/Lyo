using Lyo.Exceptions;
using Lyo.Metrics;
using Polly.Registry;

namespace Lyo.Resilience;

/// <summary>Executes actions through named resilience pipelines from the registry.</summary>
public sealed class ResilientExecutor : IResilientExecutor
{
    private readonly IMetrics _metrics;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;

    /// <summary>Creates a new resilient executor.</summary>
    /// <param name="pipelineProvider">The pipeline provider to resolve pipelines by name.</param>
    /// <param name="metrics">Optional metrics for execution duration and success/failure (uses NullMetrics if not provided).</param>
    public ResilientExecutor(ResiliencePipelineProvider<string> pipelineProvider, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(pipelineProvider, nameof(pipelineProvider));
        _pipelineProvider = pipelineProvider;
        _metrics = metrics ?? NullMetrics.Instance;
    }

    /// <inheritdoc />
    public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) => ExecuteAsync(PipelineNames.Basic, action, ct);

    /// <inheritdoc />
    public async Task ExecuteAsync(string pipelineName, Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        pipelineName ??= PipelineNames.Basic;
        var tags = new[] { (Constants.Metrics.PipelineTag, pipelineName) };
        using var timer = _metrics.StartTimer(Constants.Metrics.ExecutionDuration, tags);
        try {
            var pipeline = _pipelineProvider.GetPipeline(pipelineName);
            await pipeline.ExecuteAsync(
                    async ct => {
                        await action(ct).ConfigureAwait(false);
                    }, ct)
                .ConfigureAwait(false);

            _metrics.IncrementCounter(Constants.Metrics.ExecutionSuccess, tags: tags);
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(Constants.Metrics.ExecutionFailure, tags: tags);
            _metrics.RecordError(Constants.Metrics.ExecutionError, ex, tags);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default) => ExecuteAsync(PipelineNames.Basic, action, ct);

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(string pipelineName, Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        pipelineName ??= PipelineNames.Basic;
        var tags = new[] { (Constants.Metrics.PipelineTag, pipelineName) };
        using var timer = _metrics.StartTimer(Constants.Metrics.ExecutionDuration, tags);
        try {
            var pipeline = _pipelineProvider.GetPipeline(pipelineName);
            var result = await pipeline.ExecuteAsync(async ct => await action(ct).ConfigureAwait(false), ct).ConfigureAwait(false);
            _metrics.IncrementCounter(Constants.Metrics.ExecutionSuccess, tags: tags);
            return result;
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(Constants.Metrics.ExecutionFailure, tags: tags);
            _metrics.RecordError(Constants.Metrics.ExecutionError, ex, tags);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, Func<T, bool> isSuccess, CancellationToken ct = default)
        => ExecuteAsync(PipelineNames.Basic, action, isSuccess, ct);

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(string pipelineName, Func<CancellationToken, Task<T>> action, Func<T, bool> isSuccess, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(isSuccess, nameof(isSuccess));
        pipelineName ??= PipelineNames.Basic;
        var tags = new[] { (Constants.Metrics.PipelineTag, pipelineName) };
        using var timer = _metrics.StartTimer(Constants.Metrics.ExecutionDuration, tags);
        try {
            var pipeline = _pipelineProvider.GetPipeline(pipelineName);
            var result = await pipeline.ExecuteAsync(
                    async ct => {
                        var value = await action(ct).ConfigureAwait(false);
                        if (!isSuccess(value))
                            throw new RetryableResultException();

                        return value;
                    }, ct)
                .ConfigureAwait(false);

            _metrics.IncrementCounter(Constants.Metrics.ExecutionSuccess, tags: tags);
            return result;
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(Constants.Metrics.ExecutionFailure, tags: tags);
            _metrics.RecordError(Constants.Metrics.ExecutionError, ex, tags);
            throw;
        }
    }
}