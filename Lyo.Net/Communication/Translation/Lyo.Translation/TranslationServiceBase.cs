using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Translation.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.Translation.TranslationErrorCodes;

namespace Lyo.Translation;

/// <summary>Shared base for translation providers; implements the common bulk-translate path.</summary>
public abstract class TranslationServiceBase : ITranslationService, IDisposable
{
    private int _disposedInt;

    /// <summary>Logger used by this service.</summary>
    protected ILogger Logger { get; }

    /// <summary>Options this service was constructed with.</summary>
    protected TranslationServiceOptions Options { get; }

    /// <summary>Metrics sink (null when metrics are turned off).</summary>
    protected IMetrics Metrics { get; }

    /// <summary>Caps how many items <see cref="TranslateBulkAsync" /> runs at once.</summary>
    protected SemaphoreSlim BulkTranslationSemaphore { get; }

    /// <summary>Maps logical slots (keys from <see cref="Constants.Metrics" />) onto provider-specific metric names.</summary>
    protected Dictionary<string, string> MetricNames { get; }

    /// <summary>Constructs the <see cref="TranslationServiceBase" />.</summary>
    /// <param name="options">Service options.</param>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="metrics">Optional metrics sink for translation work.</param>
    protected TranslationServiceBase(TranslationServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
    {
        Options = options;
        Logger = logger ?? NullLogger.Instance;
        Metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        BulkTranslationSemaphore = new(options.BulkTranslationConcurrencyLimit, options.BulkTranslationConcurrencyLimit);
        // ReSharper disable once VirtualMemberCallInConstructor
        MetricNames = CreateMetricNamesDictionary();
    }

    /// <summary>Frees unmanaged resources held by TranslationServiceBase and, when Dispose(true) is used, managed ones too.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public Task<TranslationResult> TranslateAsync(string text, LanguageCodeInfo targetLanguageCode, LanguageCodeInfo? sourceLanguage = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text);
        var request = new TranslationRequest { Text = text, TargetLanguageCode = targetLanguageCode, SourceLanguage = sourceLanguage ?? Options.DefaultSourceLanguage };
        return TranslateAsync(request, ct);
    }

    /// <inheritdoc />
    public async Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
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
    /// <remarks>Results land in a concurrent bag, so order may not follow <paramref name="requests" />.</remarks>
    public async Task<IReadOnlyList<TranslationResult>> TranslateBulkAsync(IEnumerable<TranslationRequest> requests, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(requests);
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

    /// <summary>Builds the metric-name map. Derived types can override to supply their own names.</summary>
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

    /// <summary>Provider-specific translate; derived classes must implement this.</summary>
    protected abstract Task<TranslationResult> TranslateCoreAsync(TranslationRequest request, CancellationToken ct = default);

    /// <summary>Fired just before a translation begins.</summary>
    public event EventHandler<TranslationTranslatingEventArgs>? Translating;

    /// <summary>Fired after a translation finishes.</summary>
    public event EventHandler<TranslationTranslatedEventArgs>? Translated;

    /// <summary>Fired just before a bulk translation begins.</summary>
    public event EventHandler<TranslationBulkTranslatingEventArgs>? BulkTranslating;

    /// <summary>Fired after a bulk translation finishes.</summary>
    public event EventHandler<TranslationBulkTranslatedEventArgs>? BulkTranslated;

    /// <summary>Invokes Translating.</summary>
    private void OnTranslating(TranslationRequest request) => Translating?.Invoke(this, new(request));

    /// <summary>Invokes Translated.</summary>
    private void OnTranslated(TranslationResult result) => Translated?.Invoke(this, new(result));

    /// <summary>Invokes BulkTranslating.</summary>
    private void OnBulkTranslating(IReadOnlyList<TranslationRequest> requests) => BulkTranslating?.Invoke(this, new(requests));

    /// <summary>Invokes BulkTranslated.</summary>
    private void OnBulkTranslated(IReadOnlyList<TranslationResult> results, TimeSpan elapsedTime) => BulkTranslated?.Invoke(this, new(results, elapsedTime));

    /// <summary>Frees unmanaged resources, and managed ones when <paramref name="disposing" /> is true.</summary>
    /// <param name="disposing">True to free managed and unmanaged resources; false for unmanaged only.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _disposedInt, 1, 0) != 0)
            return;

        if (disposing)
            BulkTranslationSemaphore.Dispose();
    }
}