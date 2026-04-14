using Lyo.Exceptions;
using Lyo.Metrics;
using Polly.Registry;

namespace Lyo.Resilience;

/// <summary>DelegatingHandler that wraps HTTP requests with a named resilience pipeline.</summary>
public sealed class ResilienceHttpHandler : DelegatingHandler
{
    private readonly IMetrics _metrics;
    private readonly string _pipelineName;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;

    /// <summary>Creates a new resilience HTTP handler.</summary>
    /// <param name="pipelineProvider">The pipeline provider to resolve the pipeline by name.</param>
    /// <param name="pipelineName">The name of the pipeline to use.</param>
    /// <param name="metrics">Optional metrics for execution duration and success/failure (uses NullMetrics if not provided).</param>
    public ResilienceHttpHandler(ResiliencePipelineProvider<string> pipelineProvider, string pipelineName, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pipelineName, nameof(pipelineName));
        _pipelineProvider = ArgumentHelpers.ThrowIfNullReturn(pipelineProvider, nameof(pipelineProvider));
        _pipelineName = pipelineName;
        _metrics = metrics ?? NullMetrics.Instance;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var tags = new[] { (Constants.Metrics.PipelineTag, _pipelineName) };
        using var timer = _metrics.StartTimer(Constants.Metrics.ExecutionDuration, tags);
        try {
            var pipeline = _pipelineProvider.GetPipeline(_pipelineName);
            var response = await pipeline.ExecuteAsync(async ct => await base.SendAsync(request, ct).ConfigureAwait(false), ct).ConfigureAwait(false);
            _metrics.IncrementCounter(Constants.Metrics.ExecutionSuccess, tags: tags);
            return response;
        }
        catch (Exception ex) {
            _metrics.IncrementCounter(Constants.Metrics.ExecutionFailure, tags: tags);
            _metrics.RecordError(Constants.Metrics.ExecutionError, ex, tags);
            throw;
        }
    }
}