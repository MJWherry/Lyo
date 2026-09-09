using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Tts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.Tts.TtsErrorCodes;

namespace Lyo.Tts;

/// <summary>Shared base for TTS providers; implements the common bulk-synthesize path.</summary>
public abstract class TtsServiceBase<TRequest> : ITtsService<TRequest>, IDisposable
    where TRequest : TtsRequest
{
    private int _disposedInt;

    /// <summary>Logger used by this service.</summary>
    protected ILogger Logger { get; }

    /// <summary>Options this service was constructed with.</summary>
    protected TtsServiceOptions Options { get; }

    /// <summary>Metrics sink (null when metrics are turned off).</summary>
    private IMetrics Metrics { get; }

    /// <summary>Caps how many items <see cref="SynthesizeBulkAsync" /> runs at once.</summary>
    private SemaphoreSlim BulkTtsSemaphore { get; }

    /// <summary>Maps logical slots (keys from <see cref="Constants.Metrics" />) onto emitter-specific names; subclasses can replace values to namespace metrics per provider.</summary>
    /// <remarks>
    /// <para>Safe to read from any thread after construction. Change it only during construction.</para>
    /// </remarks>
    protected ConcurrentDictionary<string, string> MetricNames { get; }

    /// <summary>Stores options, attaches logging and metrics, and sets up bulk concurrency.</summary>
    /// <param name="options">Shared TTS behaviour (limits, metrics toggle, defaults).</param>
    /// <param name="logger">Optional logger; the null logger is used when omitted.</param>
    /// <param name="metrics">Optional metrics; when null or metrics are off in options, a no-op sink is used.</param>
    protected TtsServiceBase(TtsServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
    {
        Options = options;
        Logger = logger ?? NullLogger.Instance;
        Metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        BulkTtsSemaphore = new(options.BulkTtsConcurrencyLimit, options.BulkTtsConcurrencyLimit);
        // ReSharper disable once VirtualMemberCallInConstructor
        MetricNames = CreateMetricNamesDictionary();
    }

    /// <summary>Frees unmanaged resources held by TtsServiceBase and, when Dispose(true) is used, managed ones too.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public abstract Task<TtsResult<TRequest>> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default);

    /// <inheritdoc />
    public async Task<TtsResult<TRequest>> SynthesizeAsync(TRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.SynthesizeDuration)]);
        OnSynthesizing(request);
        var result = await SynthesizeCoreAsync(request, ct).ConfigureAwait(false);
        OnSynthesized(result);
        Metrics.IncrementCounter(result.IsSuccess ? MetricNames[nameof(Constants.Metrics.SynthesizeSuccess)] : MetricNames[nameof(Constants.Metrics.SynthesizeFailure)]);
        var firstEx = result.Errors?.FirstOrDefault()?.Exception;
        if (firstEx is not null)
            Metrics.RecordError(MetricNames[nameof(Constants.Metrics.SynthesizeDuration)], firstEx);

        return result;
    }

    /// <summary>Speaks text and writes audio to a file. Format is inferred from the extension.</summary>
    /// <param name="text">Text to speak.</param>
    /// <param name="outputFilePath">Destination path for the audio file.</param>
    /// <param name="voiceId">Optional voice id.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>A TtsResult for success or failure.</returns>
    public async Task<TtsResult<TRequest>> SynthesizeToFileAsync(string text, string outputFilePath, string? voiceId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath);
        var result = await SynthesizeAsync(text, voiceId, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.AudioData != null) {
#if NETSTANDARD2_0
            File.WriteAllBytes(outputFilePath, result.AudioData);
#else
            await File.WriteAllBytesAsync(outputFilePath, result.AudioData, ct).ConfigureAwait(false);
#endif
        }

        return result;
    }

    /// <summary>Speaks from a request and writes audio to a file.</summary>
    /// <param name="request">TTS request.</param>
    /// <param name="outputFilePath">Destination path for the audio file.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>A TtsResult for success or failure.</returns>
    public virtual async Task<TtsResult<TRequest>> SynthesizeToFileAsync(TRequest request, string outputFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath);
        var result = await SynthesizeAsync(request, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.AudioData != null) {
#if NETSTANDARD2_0
            File.WriteAllBytes(outputFilePath, result.AudioData);
#else
            await File.WriteAllBytesAsync(outputFilePath, result.AudioData, ct).ConfigureAwait(false);
#endif
        }

        return result;
    }

    /// <summary>Speaks text and writes audio to a stream.</summary>
    /// <param name="text">Text to speak.</param>
    /// <param name="outputStream">Stream to receive audio bytes.</param>
    /// <param name="voiceId">Optional voice id.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>A TtsResult for success or failure.</returns>
    public async Task<TtsResult<TRequest>> SynthesizeToStreamAsync(string text, Stream outputStream, string? voiceId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        var result = await SynthesizeAsync(text, voiceId, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.AudioData != null)
            await outputStream.WriteAsync(result.AudioData, 0, result.AudioData.Length, ct).ConfigureAwait(false);

        return result;
    }

    /// <summary>Speaks from a request and writes audio to a stream.</summary>
    /// <param name="request">TTS request.</param>
    /// <param name="outputStream">Stream to receive audio bytes.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>A TtsResult for success or failure.</returns>
    public virtual async Task<TtsResult<TRequest>> SynthesizeToStreamAsync(TRequest request, Stream outputStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNull(outputStream);
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        ArgumentHelpers.ThrowIfNull(outputStream);
        var result = await SynthesizeAsync(request, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.AudioData != null)
            await outputStream.WriteAsync(result.AudioData, 0, result.AudioData.Length, ct).ConfigureAwait(false);

        return result;
    }

    /// <summary>Speaks many texts at once (capped by <see cref="TtsServiceOptions.BulkTtsConcurrencyLimit" />), raising bulk events and metrics.</summary>
    /// <param name="requests">One or more requests; count must not exceed <see cref="TtsServiceOptions.MaxBulkTtsLimit" />.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>One result per request (order may not match input order).</returns>
    public async Task<IReadOnlyList<TtsResult<TRequest>>> SynthesizeBulkAsync(IEnumerable<TRequest> requests, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(requests);
        var requestList = requests.ToList();
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.BulkSynthesizeDuration)]);
        var sw = Stopwatch.StartNew();
        try {
            ArgumentHelpers.ThrowIfNullOrNotInRange(requestList.Count, 1, Options.MaxBulkTtsLimit, nameof(requests));
        }
        catch (ArgumentOutsideRangeException) {
            var error = $"Bulk request exceeds maximum limit of {Options.MaxBulkTtsLimit} requests. Requested: {requestList.Count}";
            Logger.LogError(error);
            throw;
        }

        Logger.LogInformation("Starting bulk synthesis for {Count} requests", requestList.Count);
        OnBulkSynthesizing(requestList);
        var results = new ConcurrentBag<TtsResult<TRequest>>();
        var tasks = requestList.Select(async request => {
            if (ct.IsCancellationRequested) {
                results.Add(TtsResult<TRequest>.FromException(new OperationCanceledException(ct), request, TimeSpan.Zero, OperationCancelled));
                return;
            }

            await BulkTtsSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try {
                if (ct.IsCancellationRequested) {
                    results.Add(TtsResult<TRequest>.FromException(new OperationCanceledException(ct), request, TimeSpan.Zero, OperationCancelled));
                    return;
                }

                var result = await SynthesizeAsync(request, ct).ConfigureAwait(false);
                results.Add(result);
            }
            catch (OperationCanceledException) {
                results.Add(TtsResult<TRequest>.FromException(new OperationCanceledException(ct), request, TimeSpan.Zero, OperationCancelled));
            }
            finally {
                BulkTtsSemaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        sw.Stop();
        var resultsList = results.ToList();
        var successCount = resultsList.Count(r => r.IsSuccess);
        Logger.LogInformation("Bulk synthesis completed: {Success}/{Total} successful in {Elapsed}ms", successCount, requestList.Count, sw.ElapsedMilliseconds);
        OnBulkSynthesized(resultsList, sw.Elapsed);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkSynthesizeTotal)], requestList.Count);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkSynthesizeSuccess)], successCount);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkSynthesizeFailure)], requestList.Count - successCount);
        Metrics.RecordGauge(MetricNames[nameof(Constants.Metrics.BulkSynthesizeLastDurationMs)], sw.ElapsedMilliseconds);
        return resultsList;
    }

    /// <summary>Provider-specific connection probe; derived classes must implement this.</summary>
    public abstract Task<bool> TestConnectionAsync(CancellationToken ct = default);

    /// <summary>Builds the metric-name map. Derived types can override to supply their own names.</summary>
    protected virtual ConcurrentDictionary<string, string> CreateMetricNamesDictionary()
        => new() {
            [nameof(Constants.Metrics.SynthesizeDuration)] = Constants.Metrics.SynthesizeDuration,
            [nameof(Constants.Metrics.SynthesizeSuccess)] = Constants.Metrics.SynthesizeSuccess,
            [nameof(Constants.Metrics.SynthesizeFailure)] = Constants.Metrics.SynthesizeFailure,
            [nameof(Constants.Metrics.BulkSynthesizeDuration)] = Constants.Metrics.BulkSynthesizeDuration,
            [nameof(Constants.Metrics.BulkSynthesizeTotal)] = Constants.Metrics.BulkSynthesizeTotal,
            [nameof(Constants.Metrics.BulkSynthesizeSuccess)] = Constants.Metrics.BulkSynthesizeSuccess,
            [nameof(Constants.Metrics.BulkSynthesizeFailure)] = Constants.Metrics.BulkSynthesizeFailure,
            [nameof(Constants.Metrics.BulkSynthesizeLastDurationMs)] = Constants.Metrics.BulkSynthesizeLastDurationMs
        };

    /// <summary>Provider-specific synthesize; derived classes must implement this.</summary>
    protected abstract Task<TtsResult<TRequest>> SynthesizeCoreAsync(TRequest request, CancellationToken ct = default);

    /// <summary>Fired just before synthesis begins.</summary>
    public event EventHandler<TtsSynthesizingEventArgs<TRequest>>? Synthesizing;

    /// <summary>Fired after synthesis finishes.</summary>
    public event EventHandler<TtsSynthesizedEventArgs<TRequest>>? Synthesized;

    /// <summary>Fired just before a bulk synthesis begins.</summary>
    public event EventHandler<TtsBulkSynthesizingEventArgs<TRequest>>? BulkSynthesizing;

    /// <summary>Fired after a bulk synthesis finishes.</summary>
    public event EventHandler<TtsBulkSynthesizedEventArgs<TRequest>>? BulkSynthesized;

    /// <summary>Invokes Synthesizing.</summary>
    private void OnSynthesizing(TRequest request) => Synthesizing?.Invoke(this, new(request));

    /// <summary>Invokes Synthesized.</summary>
    private void OnSynthesized(TtsResult<TRequest> result) => Synthesized?.Invoke(this, new(result));

    /// <summary>Invokes BulkSynthesizing.</summary>
    private void OnBulkSynthesizing(IReadOnlyList<TRequest> requests) => BulkSynthesizing?.Invoke(this, new(requests));

    /// <summary>Invokes BulkSynthesized.</summary>
    private void OnBulkSynthesized(IReadOnlyList<TtsResult<TRequest>> results, TimeSpan elapsedTime) => BulkSynthesized?.Invoke(this, new(results, elapsedTime));

    /// <summary>Frees unmanaged resources, and managed ones when <paramref name="disposing" /> is true.</summary>
    /// <param name="disposing">True to free managed and unmanaged resources; false for unmanaged only.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _disposedInt, 1, 0) != 0)
            return;

        if (disposing)
            BulkTtsSemaphore.Dispose();
    }
}