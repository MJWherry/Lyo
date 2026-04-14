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

/// <summary>Abstract base class for TTS service implementations that provides common bulk synthesis functionality.</summary>
public abstract class TtsServiceBase<TRequest> : ITtsService<TRequest>, IDisposable
    where TRequest : TtsRequest
{
    private int _disposedInt;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger Logger { get; }

    /// <summary>Gets the TTS service options.</summary>
    protected TtsServiceOptions Options { get; }

    /// <summary>Gets the metrics instance (null if metrics are disabled).</summary>
    private IMetrics Metrics { get; }

    /// <summary>Gets the semaphore for rate limiting bulk TTS operations.</summary>
    private SemaphoreSlim BulkTtsSemaphore { get; }

    /// <summary>Gets the metric names dictionary. Derived classes can modify this dictionary to override metric names.</summary>
    /// <remarks>
    /// <para>This dictionary is thread-safe for reads after construction. Modifications should only occur during construction.</para>
    /// </remarks>
    protected ConcurrentDictionary<string, string> MetricNames { get; }

    protected TtsServiceBase(TtsServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
    {
        Options = options;
        Logger = logger ?? NullLogger.Instance;
        Metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        BulkTtsSemaphore = new(options.BulkTtsConcurrencyLimit, options.BulkTtsConcurrencyLimit);
        // ReSharper disable once VirtualMemberCallInConstructor
        MetricNames = CreateMetricNamesDictionary();
    }

    /// <summary>Releases the unmanaged resources used by the TtsServiceBase and optionally releases the managed resources.</summary>
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
        ArgumentHelpers.ThrowIfNull(request, nameof(request));
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

    /// <summary>Synthesizes text to speech and saves it to a file. The audio format is automatically detected from the file extension.</summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="outputFilePath">The path to save the audio file.</param>
    /// <param name="voiceId">Optional voice ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A TtsResult indicating success or failure.</returns>
    public async Task<TtsResult<TRequest>> SynthesizeToFileAsync(string text, string outputFilePath, string? voiceId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text, nameof(text));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath, nameof(outputFilePath));
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

    /// <summary>Synthesizes text to speech and saves it to a file using a request object.</summary>
    /// <param name="request">The TTS request.</param>
    /// <param name="outputFilePath">The path to save the audio file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A TtsResult indicating success or failure.</returns>
    public virtual async Task<TtsResult<TRequest>> SynthesizeToFileAsync(TRequest request, string outputFilePath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request, nameof(request));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputFilePath, nameof(outputFilePath));
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

    /// <summary>Synthesizes text to speech and writes it to a stream.</summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="outputStream">The stream to write audio data to.</param>
    /// <param name="voiceId">Optional voice ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A TtsResult indicating success or failure.</returns>
    public async Task<TtsResult<TRequest>> SynthesizeToStreamAsync(string text, Stream outputStream, string? voiceId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text, nameof(text));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        var result = await SynthesizeAsync(text, voiceId, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.AudioData != null)
            await outputStream.WriteAsync(result.AudioData, 0, result.AudioData.Length, ct).ConfigureAwait(false);

        return result;
    }

    /// <summary>Synthesizes text to speech and writes it to a stream using a request object.</summary>
    /// <param name="request">The TTS request.</param>
    /// <param name="outputStream">The stream to write audio data to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A TtsResult indicating success or failure.</returns>
    public virtual async Task<TtsResult<TRequest>> SynthesizeToStreamAsync(TRequest request, Stream outputStream, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request, nameof(request));
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        OperationHelpers.ThrowIfNotWritable(outputStream, $"Stream '{nameof(outputStream)}' must be writable.");
        ArgumentHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        var result = await SynthesizeAsync(request, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.AudioData != null)
            await outputStream.WriteAsync(result.AudioData, 0, result.AudioData.Length, ct).ConfigureAwait(false);

        return result;
    }

    /// <summary>Synthesizes multiple texts to speech in bulk.</summary>
    public async Task<IReadOnlyList<TtsResult<TRequest>>> SynthesizeBulkAsync(IEnumerable<TRequest> requests, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(requests, nameof(requests));
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

    /// <summary>Tests the connection to the TTS service provider (must be implemented by derived classes).</summary>
    public abstract Task<bool> TestConnectionAsync(CancellationToken ct = default);

    /// <summary>Creates the metric names dictionary. Override in derived classes to customize metric names.</summary>
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

    /// <summary>Synthesizes text to speech (must be implemented by derived classes).</summary>
    protected abstract Task<TtsResult<TRequest>> SynthesizeCoreAsync(TRequest request, CancellationToken ct = default);

    /// <summary>Occurs before synthesis starts.</summary>
    public event EventHandler<TtsSynthesizingEventArgs<TRequest>>? Synthesizing;

    /// <summary>Occurs after synthesis completes.</summary>
    public event EventHandler<TtsSynthesizedEventArgs<TRequest>>? Synthesized;

    /// <summary>Occurs before a bulk synthesis operation starts.</summary>
    public event EventHandler<TtsBulkSynthesizingEventArgs<TRequest>>? BulkSynthesizing;

    /// <summary>Occurs after a bulk synthesis operation completes.</summary>
    public event EventHandler<TtsBulkSynthesizedEventArgs<TRequest>>? BulkSynthesized;

    /// <summary>Raises the Synthesizing event.</summary>
    private void OnSynthesizing(TRequest request) => Synthesizing?.Invoke(this, new(request));

    /// <summary>Raises the Synthesized event.</summary>
    private void OnSynthesized(TtsResult<TRequest> result) => Synthesized?.Invoke(this, new(result));

    /// <summary>Raises the BulkSynthesizing event.</summary>
    private void OnBulkSynthesizing(IReadOnlyList<TRequest> requests) => BulkSynthesizing?.Invoke(this, new(requests));

    /// <summary>Raises the BulkSynthesized event.</summary>
    private void OnBulkSynthesized(IReadOnlyList<TtsResult<TRequest>> results, TimeSpan elapsedTime) => BulkSynthesized?.Invoke(this, new(results, elapsedTime));

    /// <summary>Releases the unmanaged resources used by the TtsServiceBase and optionally releases the managed resources.</summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _disposedInt, 1, 0) != 0)
            return;

        if (disposing)
            BulkTtsSemaphore.Dispose();
    }
}