using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Translation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.Translation.TranslationErrorCodes;

namespace Lyo.Translation;

/// <summary>Abstract base class for translation service implementations that provides common bulk translation functionality.</summary>
public abstract class TranslationServiceBase : ITranslationService, IDisposable
{
    private int _disposedInt;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger Logger { get; }

    /// <summary>Gets the translation service options.</summary>
    protected TranslationServiceOptions Options { get; }

    /// <summary>Gets the metrics instance (null if metrics are disabled).</summary>
    protected IMetrics Metrics { get; }

    /// <summary>Gets the semaphore for rate limiting bulk translation operations.</summary>
    protected SemaphoreSlim BulkTranslationSemaphore { get; }

    /// <summary>Gets the metric names dictionary. Derived classes can modify this dictionary to override metric names.</summary>
    protected Dictionary<string, string> MetricNames { get; }

    /// <summary>Initializes a new instance of the <see cref="TranslationServiceBase" /> class.</summary>
    /// <param name="options">The translation service options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics instance for tracking translation operations.</param>
    protected TranslationServiceBase(TranslationServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
    {
        Options = options;
        Logger = logger ?? NullLogger.Instance;
        Metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        BulkTranslationSemaphore = new(options.BulkTranslationConcurrencyLimit, options.BulkTranslationConcurrencyLimit);
        // ReSharper disable once VirtualMemberCallInConstructor
        MetricNames = CreateMetricNamesDictionary();
    }

    /// <summary>Releases the unmanaged resources used by the TranslationServiceBase and optionally releases the managed resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public Task<TranslationResult> TranslateAsync(string text, LanguageCodeInfo targetLanguageCode, LanguageCodeInfo? sourceLanguage = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text, nameof(text));
        var request = new TranslationRequest { Text = text, TargetLanguageCode = targetLanguageCode, SourceLanguage = sourceLanguage ?? Options.DefaultSourceLanguage };
        return TranslateAsync(request, ct);
    }

    /// <inheritdoc />
    public async Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request, nameof(request));
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.TranslateDuration)]);
        OnTranslating(request);
        var result = await TranslateCoreAsync(request, ct).ConfigureAwait(false);
        OnTranslated(result);
        Metrics.IncrementCounter(result.IsSuccess ? MetricNames[nameof(Constants.Metrics.TranslateSuccess)] : MetricNames[nameof(Constants.Metrics.TranslateFailure)]);
        var firstEx = result.Errors?.FirstOrDefault()?.Exception;
        if (firstEx != null)
            Metrics.RecordError(MetricNames[nameof(Constants.Metrics.TranslateDuration)], firstEx);

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TranslationResult>> TranslateBulkAsync(IEnumerable<TranslationRequest> requests, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(requests, nameof(requests));
        var requestList = requests.ToList();
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.BulkTranslateDuration)]);
        var sw = Stopwatch.StartNew();
        try {
            ArgumentHelpers.ThrowIfNullOrNotInRange(requestList.Count, 1, Options.MaxBulkTranslationLimit, nameof(requests));
        }
        catch (ArgumentOutsideRangeException) {
            var error = $"Bulk request exceeds maximum limit of {Options.MaxBulkTranslationLimit} requests. Requested: {requestList.Count}";
            Logger.LogError(error);
            throw;
        }

        Logger.LogInformation("Starting bulk translation for {Count} requests", requestList.Count);
        OnBulkTranslating(requestList);
        var results = new ConcurrentBag<TranslationResult>();
        var tasks = requestList.Select(async request => {
            if (ct.IsCancellationRequested) {
                results.Add(TranslationResult.FromException(new OperationCanceledException(ct), request, TimeSpan.Zero, OperationCancelled));
                return;
            }

            await BulkTranslationSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try {
                if (ct.IsCancellationRequested) {
                    results.Add(TranslationResult.FromException(new OperationCanceledException(ct), request, TimeSpan.Zero, OperationCancelled));
                    return;
                }

                var result = await TranslateAsync(request, ct).ConfigureAwait(false);
                results.Add(result);
            }
            catch (OperationCanceledException) {
                results.Add(TranslationResult.FromException(new OperationCanceledException(ct), request, TimeSpan.Zero, OperationCancelled));
            }
            finally {
                BulkTranslationSemaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        sw.Stop();
        var resultsList = results.ToList();
        var successCount = resultsList.Count(r => r.IsSuccess);
        Logger.LogInformation("Bulk translation completed: {Success}/{Total} successful in {Elapsed}ms", successCount, requestList.Count, sw.ElapsedMilliseconds);
        OnBulkTranslated(resultsList, sw.Elapsed);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkTranslateTotal)], requestList.Count);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkTranslateSuccess)], successCount);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkTranslateFailure)], requestList.Count - successCount);
        Metrics.RecordGauge(MetricNames[nameof(Constants.Metrics.BulkTranslateLastDurationMs)], sw.ElapsedMilliseconds);
        return resultsList;
    }

    /// <inheritdoc />
    public abstract Task<LanguageCodeInfo> DetectLanguageAsync(string text, CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<bool> TestConnectionAsync(CancellationToken ct = default);

    /// <summary>Creates the metric names dictionary. Override in derived classes to customize metric names.</summary>
    protected virtual Dictionary<string, string> CreateMetricNamesDictionary()
        => new() {
            { nameof(Constants.Metrics.TranslateDuration), Constants.Metrics.TranslateDuration },
            { nameof(Constants.Metrics.TranslateSuccess), Constants.Metrics.TranslateSuccess },
            { nameof(Constants.Metrics.TranslateFailure), Constants.Metrics.TranslateFailure },
            { nameof(Constants.Metrics.BulkTranslateDuration), Constants.Metrics.BulkTranslateDuration },
            { nameof(Constants.Metrics.BulkTranslateTotal), Constants.Metrics.BulkTranslateTotal },
            { nameof(Constants.Metrics.BulkTranslateSuccess), Constants.Metrics.BulkTranslateSuccess },
            { nameof(Constants.Metrics.BulkTranslateFailure), Constants.Metrics.BulkTranslateFailure },
            { nameof(Constants.Metrics.BulkTranslateLastDurationMs), Constants.Metrics.BulkTranslateLastDurationMs },
            { nameof(Constants.Metrics.DetectLanguageDuration), Constants.Metrics.DetectLanguageDuration },
            { nameof(Constants.Metrics.DetectLanguageSuccess), Constants.Metrics.DetectLanguageSuccess },
            { nameof(Constants.Metrics.DetectLanguageFailure), Constants.Metrics.DetectLanguageFailure }
        };

    /// <summary>Translates text (must be implemented by derived classes).</summary>
    protected abstract Task<TranslationResult> TranslateCoreAsync(TranslationRequest request, CancellationToken ct = default);

    /// <summary>Occurs before translation starts.</summary>
    public event EventHandler<TranslationTranslatingEventArgs>? Translating;

    /// <summary>Occurs after translation completes.</summary>
    public event EventHandler<TranslationTranslatedEventArgs>? Translated;

    /// <summary>Occurs before a bulk translation operation starts.</summary>
    public event EventHandler<TranslationBulkTranslatingEventArgs>? BulkTranslating;

    /// <summary>Occurs after a bulk translation operation completes.</summary>
    public event EventHandler<TranslationBulkTranslatedEventArgs>? BulkTranslated;

    /// <summary>Raises the Translating event.</summary>
    private void OnTranslating(TranslationRequest request) => Translating?.Invoke(this, new(request));

    /// <summary>Raises the Translated event.</summary>
    private void OnTranslated(TranslationResult result) => Translated?.Invoke(this, new(result));

    /// <summary>Raises the BulkTranslating event.</summary>
    private void OnBulkTranslating(IReadOnlyList<TranslationRequest> requests) => BulkTranslating?.Invoke(this, new(requests));

    /// <summary>Raises the BulkTranslated event.</summary>
    private void OnBulkTranslated(IReadOnlyList<TranslationResult> results, TimeSpan elapsedTime) => BulkTranslated?.Invoke(this, new(results, elapsedTime));

    /// <summary>Releases the unmanaged resources used by the TranslationServiceBase and optionally releases the managed resources.</summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _disposedInt, 1, 0) != 0)
            return;

        if (disposing)
            BulkTranslationSemaphore.Dispose();
    }
}