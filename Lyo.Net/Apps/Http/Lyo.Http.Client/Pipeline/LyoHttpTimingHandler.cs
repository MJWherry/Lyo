using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Http.Client.Pipeline;

/// <summary>Waits <see cref="LyoHttpTimingOptions.DelayBeforeSendMin" />–<see cref="LyoHttpTimingOptions.DelayBeforeSendMax" /> (plus jitter) after a rate-limit permit is acquired.</summary>
public sealed class LyoHttpTimingHandler : DelegatingHandler
{
    private readonly LyoHttpTimingOptions _options;
    private readonly ILogger _logger;
    private readonly Random _rng = new();

    /// <summary>Creates the handler.</summary>
    public LyoHttpTimingHandler(LyoHttpTimingOptions options, ILogger? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _options = options;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(request);
        if (_options.Enabled)
            await DelayAsync(ct).ConfigureAwait(false);

        return await base.SendAsync(request, ct).ConfigureAwait(false);
    }

    private async Task DelayAsync(CancellationToken ct)
    {
        var min = _options.DelayBeforeSendMin.TotalMilliseconds;
        var max = _options.DelayBeforeSendMax.TotalMilliseconds;
        if (max <= 0 && min <= 0)
            return;

        var sample = min >= max ? min : min + _rng.NextDouble() * (max - min);
        if (_options.Jitter > 0)
            sample *= 1 + ((_options.Jitter * 2) * _rng.NextDouble() - _options.Jitter);

        var delay = TimeSpan.FromMilliseconds(Math.Max(0, sample));
        if (delay <= TimeSpan.Zero)
            return;

        _logger.LogDebug("HTTP timing delay {Delay}", delay);
        await Task.Delay(delay, ct).ConfigureAwait(false);
    }
}
