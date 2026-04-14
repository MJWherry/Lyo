using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Stt.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Stt;

/// <summary>Abstract base class for STT service implementations that provides common bulk recognition functionality.</summary>
public abstract class SttServiceBase : ISttService, IDisposable
{
    private int _disposedInt;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger Logger { get; }

    /// <summary>Gets the STT service options.</summary>
    protected SttServiceOptions Options { get; }

    /// <summary>Gets the metrics instance (null if metrics are disabled).</summary>
    protected IMetrics Metrics { get; }

    /// <summary>Gets the semaphore for rate limiting bulk STT operations.</summary>
    protected SemaphoreSlim BulkSttSemaphore { get; }

    /// <summary>Gets the metric names dictionary. Derived classes can modify this dictionary to override metric names.</summary>
    protected Dictionary<string, string> MetricNames { get; }

    /// <summary>Initializes a new instance of the <see cref="SttServiceBase" /> class.</summary>
    /// <param name="options">The STT service options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics instance for tracking STT operations.</param>
    protected SttServiceBase(SttServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
    {
        Options = options;
        Logger = logger ?? NullLogger.Instance;
        Metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        BulkSttSemaphore = new(options.BulkSttConcurrencyLimit, options.BulkSttConcurrencyLimit);
        MetricNames = CreateMetricNamesDictionary();
    }

    /// <summary>Releases the unmanaged resources used by the SttServiceBase and optionally releases the managed resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public Task<SttResult> RecognizeAsync(byte[] audioData, LanguageCodeInfo? languageCode = null, AudioFormat? audioFormat = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(audioData, nameof(audioData));
        ArgumentHelpers.ThrowIfNullOrEmpty(audioData, nameof(audioData));
        var request = new SttRequest { AudioData = audioData, LanguageCode = languageCode ?? Options.DefaultLanguageCode, AudioFormat = audioFormat ?? Options.DefaultAudioFormat };
        return RecognizeAsync(request, ct);
    }

    /// <inheritdoc />
    public async Task<SttResult> RecognizeFromFileAsync(string audioFilePath, LanguageCodeInfo? languageCode = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(audioFilePath, nameof(audioFilePath));
#if NETSTANDARD2_0
        var audioData = File.ReadAllBytes(audioFilePath);
#else
        var audioData = await File.ReadAllBytesAsync(audioFilePath, ct).ConfigureAwait(false);
#endif
        var detectedFormat = audioFilePath.GetAudioFormatFromExtension();
        var request = new SttRequest {
            AudioData = audioData,
            AudioFilePath = audioFilePath,
            LanguageCode = languageCode ?? Options.DefaultLanguageCode,
            AudioFormat = detectedFormat != AudioFormat.Unknown ? detectedFormat : Options.DefaultAudioFormat
        };

        return await RecognizeAsync(request, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SttResult> RecognizeFromStreamAsync(
        Stream audioStream,
        LanguageCodeInfo? languageCode = null,
        AudioFormat? audioFormat = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(audioStream, nameof(audioStream));
        OperationHelpers.ThrowIfNotReadable(audioStream, $"Stream '{nameof(audioStream)}' must be readable.");
        using var memoryStream = new MemoryStream();
#if NETSTANDARD2_0
        await audioStream.CopyToAsync(memoryStream).ConfigureAwait(false);
#else
        await audioStream.CopyToAsync(memoryStream, ct).ConfigureAwait(false);
#endif
        var audioData = memoryStream.ToArray();
        var request = new SttRequest { AudioData = audioData, LanguageCode = languageCode ?? Options.DefaultLanguageCode, AudioFormat = audioFormat ?? Options.DefaultAudioFormat };
        return await RecognizeAsync(request, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SttResult> RecognizeAsync(SttRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request, nameof(request));
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.RecognizeDuration)]);
        OnRecognizing(request);
        var result = await RecognizeCoreAsync(request, ct).ConfigureAwait(false);
        OnRecognized(result);
        Metrics.IncrementCounter(result.IsSuccess ? MetricNames[nameof(Constants.Metrics.RecognizeSuccess)] : MetricNames[nameof(Constants.Metrics.RecognizeFailure)]);
        if (result.Exception != null)
            Metrics.RecordError(MetricNames[nameof(Constants.Metrics.RecognizeDuration)], result.Exception);

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SttResult>> RecognizeBulkAsync(IEnumerable<SttRequest> requests, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(requests, nameof(requests));
        var requestList = requests.ToList();
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.BulkRecognizeDuration)]);
        var sw = Stopwatch.StartNew();
        try {
            ArgumentHelpers.ThrowIfNullOrNotInRange(requestList.Count, 1, Options.MaxBulkSttLimit, nameof(requests));
        }
        catch (ArgumentOutsideRangeException) {
            var error = $"Bulk request exceeds maximum limit of {Options.MaxBulkSttLimit} requests. Requested: {requestList.Count}";
            Logger.LogError(error);
            throw;
        }

        Logger.LogInformation("Starting bulk recognition for {Count} requests", requestList.Count);
        OnBulkRecognizing(requestList);
        var results = new ConcurrentBag<SttResult>();
        var tasks = requestList.Select(async request => {
            if (ct.IsCancellationRequested) {
                results.Add(SttResult.Failure("Operation cancelled", new OperationCanceledException(ct), TimeSpan.Zero, request));
                return;
            }

            await BulkSttSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try {
                if (ct.IsCancellationRequested) {
                    results.Add(SttResult.Failure("Operation cancelled", new OperationCanceledException(ct), TimeSpan.Zero, request));
                    return;
                }

                var result = await RecognizeAsync(request, ct).ConfigureAwait(false);
                results.Add(result);
            }
            catch (OperationCanceledException) {
                results.Add(SttResult.Failure("Operation cancelled", new OperationCanceledException(ct), TimeSpan.Zero, request));
            }
            finally {
                BulkSttSemaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        sw.Stop();
        var resultsList = results.ToList();
        var successCount = resultsList.Count(r => r.IsSuccess);
        Logger.LogInformation("Bulk recognition completed: {Success}/{Total} successful in {Elapsed}ms", successCount, requestList.Count, sw.ElapsedMilliseconds);
        OnBulkRecognized(resultsList, sw.Elapsed);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkRecognizeTotal)], requestList.Count);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkRecognizeSuccess)], successCount);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkRecognizeFailure)], requestList.Count - successCount);
        Metrics.RecordGauge(MetricNames[nameof(Constants.Metrics.BulkRecognizeLastDurationMs)], sw.ElapsedMilliseconds);
        return resultsList;
    }

    /// <inheritdoc />
    public abstract Task<bool> TestConnectionAsync(CancellationToken ct = default);

    /// <summary>Creates the metric names dictionary. Override in derived classes to customize metric names.</summary>
    protected virtual Dictionary<string, string> CreateMetricNamesDictionary()
        => new() {
            { nameof(Constants.Metrics.RecognizeDuration), Constants.Metrics.RecognizeDuration },
            { nameof(Constants.Metrics.RecognizeSuccess), Constants.Metrics.RecognizeSuccess },
            { nameof(Constants.Metrics.RecognizeFailure), Constants.Metrics.RecognizeFailure },
            { nameof(Constants.Metrics.BulkRecognizeDuration), Constants.Metrics.BulkRecognizeDuration },
            { nameof(Constants.Metrics.BulkRecognizeTotal), Constants.Metrics.BulkRecognizeTotal },
            { nameof(Constants.Metrics.BulkRecognizeSuccess), Constants.Metrics.BulkRecognizeSuccess },
            { nameof(Constants.Metrics.BulkRecognizeFailure), Constants.Metrics.BulkRecognizeFailure },
            { nameof(Constants.Metrics.BulkRecognizeLastDurationMs), Constants.Metrics.BulkRecognizeLastDurationMs }
        };

    /// <summary>Recognizes speech (must be implemented by derived classes).</summary>
    protected abstract Task<SttResult> RecognizeCoreAsync(SttRequest request, CancellationToken ct = default);

    /// <summary>Occurs before recognition starts.</summary>
    public event EventHandler<SttRecognizingEventArgs>? Recognizing;

    /// <summary>Occurs after recognition completes.</summary>
    public event EventHandler<SttRecognizedEventArgs>? Recognized;

    /// <summary>Occurs before a bulk recognition operation starts.</summary>
    public event EventHandler<SttBulkRecognizingEventArgs>? BulkRecognizing;

    /// <summary>Occurs after a bulk recognition operation completes.</summary>
    public event EventHandler<SttBulkRecognizedEventArgs>? BulkRecognized;

    /// <summary>Raises the Recognizing event.</summary>
    private void OnRecognizing(SttRequest request) => Recognizing?.Invoke(this, new(request));

    /// <summary>Raises the Recognized event.</summary>
    private void OnRecognized(SttResult result) => Recognized?.Invoke(this, new(result));

    /// <summary>Raises the BulkRecognizing event.</summary>
    private void OnBulkRecognizing(IReadOnlyList<SttRequest> requests) => BulkRecognizing?.Invoke(this, new(requests));

    /// <summary>Raises the BulkRecognized event.</summary>
    private void OnBulkRecognized(IReadOnlyList<SttResult> results, TimeSpan elapsedTime) => BulkRecognized?.Invoke(this, new(results, elapsedTime));

    /// <summary>Releases the unmanaged resources used by the SttServiceBase and optionally releases the managed resources.</summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _disposedInt, 1, 0) != 0)
            return;

        if (disposing)
            BulkSttSemaphore.Dispose();
    }
}