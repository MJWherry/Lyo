using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Extensions;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Stt.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Stt;

/// <summary>Shared base for STT providers; implements the common bulk-recognize path.</summary>
public abstract class SttServiceBase : ISttService, IDisposable
{
    private int _disposedInt;

    /// <summary>Logger used by this service.</summary>
    protected ILogger Logger { get; }

    /// <summary>Options this service was constructed with.</summary>
    protected SttServiceOptions Options { get; }

    /// <summary>Metrics sink (null when metrics are turned off).</summary>
    protected IMetrics Metrics { get; }

    /// <summary>Semaphore that rate-limits bulk STT work.</summary>
    protected SemaphoreSlim BulkSttSemaphore { get; }

    /// <summary>Metric-name map. Derived types may change entries to override names.</summary>
    protected Dictionary<string, string> MetricNames { get; }

    /// <summary>Constructs the <see cref="SttServiceBase" />.</summary>
    /// <param name="options">Service options.</param>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="metrics">Optional metrics sink for STT work.</param>
    protected SttServiceBase(SttServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
    {
        Options = options;
        Logger = logger ?? NullLogger.Instance;
        Metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        BulkSttSemaphore = new(options.BulkSttConcurrencyLimit, options.BulkSttConcurrencyLimit);
        MetricNames = CreateMetricNamesDictionary();
    }

    /// <summary>Frees unmanaged resources held by SttServiceBase and, when Dispose(true) is used, managed ones too.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public Task<SttResult> RecognizeAsync(byte[] audioData, LanguageCodeInfo? languageCode = null, AudioFormat? audioFormat = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(audioData);
        ArgumentHelpers.ThrowIfNullOrEmpty(audioData);
        var request = new SttRequest { AudioData = audioData, LanguageCode = languageCode ?? Options.DefaultLanguageCode, AudioFormat = audioFormat ?? Options.DefaultAudioFormat };
        return RecognizeAsync(request, ct);
    }

    /// <inheritdoc />
    public async Task<SttResult> RecognizeFromFileAsync(string audioFilePath, LanguageCodeInfo? languageCode = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(audioFilePath);
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
        ArgumentHelpers.ThrowIfNull(audioStream);
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
        ArgumentHelpers.ThrowIfNull(request);
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
        ArgumentHelpers.ThrowIfNull(requests);
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

    /// <summary>Builds the metric-name map. Derived types can override to supply their own names.</summary>
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

    /// <summary>Provider-specific recognize; derived classes must implement this.</summary>
    protected abstract Task<SttResult> RecognizeCoreAsync(SttRequest request, CancellationToken ct = default);

    /// <summary>Fired just before recognition begins.</summary>
    public event EventHandler<SttRecognizingEventArgs>? Recognizing;

    /// <summary>Fired after recognition finishes.</summary>
    public event EventHandler<SttRecognizedEventArgs>? Recognized;

    /// <summary>Fired just before a bulk recognition begins.</summary>
    public event EventHandler<SttBulkRecognizingEventArgs>? BulkRecognizing;

    /// <summary>Fired after a bulk recognition finishes.</summary>
    public event EventHandler<SttBulkRecognizedEventArgs>? BulkRecognized;

    /// <summary>Invokes Recognizing.</summary>
    private void OnRecognizing(SttRequest request) => Recognizing?.Invoke(this, new(request));

    /// <summary>Invokes Recognized.</summary>
    private void OnRecognized(SttResult result) => Recognized?.Invoke(this, new(result));

    /// <summary>Invokes BulkRecognizing.</summary>
    private void OnBulkRecognizing(IReadOnlyList<SttRequest> requests) => BulkRecognizing?.Invoke(this, new(requests));

    /// <summary>Invokes BulkRecognized.</summary>
    private void OnBulkRecognized(IReadOnlyList<SttResult> results, TimeSpan elapsedTime) => BulkRecognized?.Invoke(this, new(results, elapsedTime));

    /// <summary>Frees unmanaged resources, and managed ones when <paramref name="disposing" /> is true.</summary>
    /// <param name="disposing">True to free managed and unmanaged resources; false for unmanaged only.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _disposedInt, 1, 0) != 0)
            return;

        if (disposing)
            BulkSttSemaphore.Dispose();
    }
}