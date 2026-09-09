using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Metrics;

namespace Lyo.Http.Client.Pipeline;

/// <summary>Records duration and status for each send. Outer than timing.</summary>
public sealed class LyoHttpMetricsHandler : DelegatingHandler
{
    private readonly IMetrics _metrics;

    /// <summary>Creates the handler. Uses <see cref="NullMetrics" /> when <paramref name="metrics" /> is null.</summary>
    public LyoHttpMetricsHandler(IMetrics? metrics = null)
        => _metrics = metrics ?? NullMetrics.Instance;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var tags = new (string, string)[] {
            ("method", request.Method.Method),
            ("host", request.RequestUri?.Host ?? "unknown")
        };
        var sw = Stopwatch.StartNew();
        try {
            var response = await base.SendAsync(request, ct).ConfigureAwait(false);
            sw.Stop();
            var status = ((int)response.StatusCode).ToString();
            _metrics.RecordTiming("http.client.duration", sw.Elapsed, tags.Concat([("status", status)]));
            _metrics.IncrementCounter("http.client.requests", tags: tags.Concat([("status", status)]));
            return response;
        }
        catch (Exception ex) {
            sw.Stop();
            _metrics.RecordTiming("http.client.duration", sw.Elapsed, tags.Concat([("status", "error")]));
            _metrics.RecordError("http.client.error", ex, tags);
            throw;
        }
    }
}
