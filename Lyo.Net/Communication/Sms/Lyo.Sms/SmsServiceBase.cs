using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Result;
using Lyo.Sms.Builders;
using Lyo.Sms.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Sms;

/// <summary>Shared base for SMS providers; implements the common bulk-send path.</summary>
/// <typeparam name="TResult">Send-result type (for example <see cref="Result{SmsRequest}" /> or a provider subtype).</typeparam>
public abstract class SmsServiceBase<TResult> : ISmsService<TResult>, IDisposable
    where TResult : Result<SmsRequest>
{
    private int _disposedInt;

    /// <summary>Logger used by this service.</summary>
    protected ILogger Logger { get; }

    /// <summary>Options this service was constructed with.</summary>
    protected SmsServiceOptions Options { get; }

    /// <summary>Metrics sink (null when metrics are turned off).</summary>
    protected IMetrics Metrics { get; }

    /// <summary>Metric-name map. Derived types may change entries to override names.</summary>
    protected Dictionary<string, string> MetricNames { get; }

    /// <summary>Semaphore that rate-limits bulk SMS work.</summary>
    protected SemaphoreSlim BulkSmsSemaphore { get; }

    /// <summary>Constructs the <see cref="SmsServiceBase{TResult}" />.</summary>
    /// <param name="options">Service options.</param>
    /// <param name="logger">Logger to write to.</param>
    /// <param name="metrics">Optional metrics sink for SMS work.</param>
    protected SmsServiceBase(SmsServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        Options = options;
        Logger = logger ?? NullLogger.Instance;
        Metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        BulkSmsSemaphore = new(options.BulkSmsConcurrencyLimit, options.BulkSmsConcurrencyLimit);
        MetricNames = CreateMetricNamesDictionary();
    }

    /// <summary>Frees unmanaged resources held by SmsServiceBase and, when Dispose(true) is used, managed ones too.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async Task<TResult> SendAsync(SmsRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.SendDuration)]);
        OnMessageSending(request);
        var result = await SendCoreAsync(request, ct).ConfigureAwait(false);
        OnMessageSent(result);
        Metrics.IncrementCounter(result.IsSuccess ? MetricNames[nameof(Constants.Metrics.SendSuccess)] : MetricNames[nameof(Constants.Metrics.SendFailure)]);
        var firstException = result.Errors?.FirstOrDefault()?.Exception;
        if (firstException != null)
            Metrics.RecordError(MetricNames[nameof(Constants.Metrics.SendDuration)], firstException);

        return result;
    }

    /// <inheritdoc />
    public abstract Task<TResult> GetMessageByIdAsync(string messageId, CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<SmsMessageQueryResults<TResult>> GetMessagesAsync(SmsMessageQueryFilter filter, CancellationToken ct = default);

    /// <inheritdoc />
    public Task<bool> TestConnectionAsync(CancellationToken ct = default) => TestConnectionCoreAsync(ct);

    /// <inheritdoc />
    public Task<TResult> SendSmsAsync(string to, string body, string? from = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to);
        ArgumentHelpers.ThrowIfNull(body);
        var builder = SmsMessageBuilder.New().SetTo(to).SetBody(body);
        if (!from.IsNullOrWhitespace())
            builder.SetFrom(from);
        else {
            var defaultFrom = Options.DefaultFromPhoneNumber;
            if (!defaultFrom.IsNullOrWhitespace())
                builder.SetFrom(defaultFrom);
        }

        return SendAsync(builder, null, ct);
    }

    /// <inheritdoc />
    public Task<TResult> SendAsync(SmsMessageBuilder builder, string? from = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(builder);
        var message = builder.Build();
        if (!string.IsNullOrWhiteSpace(from))
            message.From = from;
        else if (string.IsNullOrWhiteSpace(message.From)) {
            var defaultFrom = GetDefaultFromPhoneNumber();
            if (!string.IsNullOrWhiteSpace(defaultFrom))
                message.From = defaultFrom;
        }

        return SendAsync(message, ct);
    }

    /// <inheritdoc />
    public Task<TResult> SendMmsAsync(string to, IEnumerable<string> mediaUrls, string? body = null, string? from = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to);
        var mediaUrlList = mediaUrls.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(mediaUrlList, nameof(mediaUrls));
        var uris = mediaUrlList.Select(url => UriHelpers.GetValidUri(url)).ToList();
        return SendMmsAsync(to, uris, body, from, ct);
    }

    /// <inheritdoc />
    public Task<TResult> SendMmsAsync(string to, IEnumerable<Uri> mediaUrls, string? body = null, string? from = null, CancellationToken ct = default)
    {
        var mediaUrlList = mediaUrls.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(mediaUrlList, nameof(mediaUrls));
        var builder = SmsMessageBuilder.New().SetTo(to);
        if (!body.IsNullOrWhitespace())
            builder.SetBody(body);

        if (!from.IsNullOrWhitespace())
            builder.SetFrom(from);
        else {
            var defaultFrom = GetDefaultFromPhoneNumber();
            if (!defaultFrom.IsNullOrWhitespace())
                builder.SetFrom(defaultFrom);
        }

        foreach (var mediaUrl in mediaUrlList)
            builder.AddAttachment(mediaUrl.ToString());

        return SendAsync(builder, from, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TResult>> SendBulkAsync(IEnumerable<SmsMessageBuilder> builders, CancellationToken ct = default)
    {
        var builderList = builders.ToList();
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.BulkSendDuration)]);
        var sw = Stopwatch.StartNew();
        try {
            ArgumentHelpers.ThrowIfNullOrNotInRange(builderList.Count, 1, Options.MaxBulkSmsLimit, nameof(builders));
        }
        catch (ArgumentOutsideRangeException) {
            var error = $"Bulk request exceeds maximum limit of {Options.MaxBulkSmsLimit} messages. Requested: {builderList.Count}";
            Logger.LogError(error);
            throw;
        }

        Logger.LogInformation("Starting bulk send for {Count} messages", builderList.Count);
        var results = new ConcurrentBag<TResult>();
        var messageList = new List<SmsRequest>();
        var builderMessageMap = new Dictionary<SmsMessageBuilder, (SmsRequest? Message, Exception? BuildException)>();
        foreach (var builder in builderList) {
            try {
                var message = builder.Build();
                messageList.Add(message);
                builderMessageMap[builder] = (message, null);
            }
            catch (Exception ex) {
                builderMessageMap[builder] = (null, ex);
            }
        }

        if (messageList.Count > 0)
            OnBulkSending(messageList);

        var tasks = builderList.Select(async builder => {
            var (message, buildException) = builderMessageMap[builder];
            if (buildException != null) {
                var failedMessage = new SmsRequest { To = "unknown", Body = string.Empty, From = null };
                results.Add(CreateFailure(buildException, SmsErrorCodes.BuildFailed, failedMessage));
                return;
            }

            if (message == null) {
                var failedMessage = new SmsRequest { To = "unknown", Body = string.Empty, From = null };
                results.Add(CreateFailure(new InvalidOperationException("Message was not built"), SmsErrorCodes.MessageNotBuilt, failedMessage));
                return;
            }

            if (ct.IsCancellationRequested) {
                results.Add(CreateFailure(new OperationCanceledException(ct), SmsErrorCodes.OperationCancelled, message));
                return;
            }

            await BulkSmsSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try {
                if (ct.IsCancellationRequested) {
                    results.Add(CreateFailure(new OperationCanceledException(ct), SmsErrorCodes.OperationCancelled, message));
                    return;
                }

                var result = await SendAsync(builder, null, ct).ConfigureAwait(false);
                results.Add(result);
            }
            catch (OperationCanceledException) {
                results.Add(CreateFailure(new OperationCanceledException(ct), SmsErrorCodes.OperationCancelled, message));
            }
            finally {
                BulkSmsSemaphore.Release();
            }
        });

        // Wait until every task finishes
        await Task.WhenAll(tasks).ConfigureAwait(false);
        sw.Stop();
        var resultsList = results.ToList();
        var successCount = resultsList.Count(r => r.IsSuccess);
        Logger.LogInformation("Bulk send completed: {Success}/{Total} successful in {Elapsed}ms", successCount, builderList.Count, sw.ElapsedMilliseconds);
        OnBulkSent(resultsList);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkSendTotal)], builderList.Count);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkSendSuccess)], successCount);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkSendFailure)], builderList.Count - successCount);
        Metrics.RecordGauge(MetricNames[nameof(Constants.Metrics.BulkSendLastDurationMs)], sw.ElapsedMilliseconds);
        return resultsList;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TResult>> SendBulkSmsAsync(IEnumerable<SmsRequest> messages, CancellationToken ct = default)
    {
        var messageList = messages.ToList();
        using var timer = Metrics.StartTimer(MetricNames[nameof(Constants.Metrics.BulkSendDuration)]);
        var sw = Stopwatch.StartNew();
        try {
            ArgumentHelpers.ThrowIfNullOrNotInRange(messageList.Count, 1, Options.MaxBulkSmsLimit, nameof(messages));
        }
        catch (ArgumentOutsideRangeException) {
            var error = $"Bulk SMS request exceeds maximum limit of {Options.MaxBulkSmsLimit} messages. Requested: {messageList.Count}";
            Logger.LogError(error);
            throw;
        }

        Logger.LogInformation("Starting bulk SMS send for {Count} messages", messageList.Count);
        OnBulkSending(messageList);
        var results = new ConcurrentBag<TResult>();
        var tasks = messageList.Select(async message => {
            if (ct.IsCancellationRequested) {
                results.Add(CreateFailure(new OperationCanceledException(ct), SmsErrorCodes.OperationCancelled, message));
                return;
            }

            await BulkSmsSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try {
                if (ct.IsCancellationRequested) {
                    results.Add(CreateFailure(new OperationCanceledException(ct), SmsErrorCodes.OperationCancelled, message));
                    return;
                }

                var result = await SendAsync(message, ct).ConfigureAwait(false);
                results.Add(result);
            }
            catch (OperationCanceledException) {
                results.Add(CreateFailure(new OperationCanceledException(ct), SmsErrorCodes.OperationCancelled, message));
            }
            finally {
                BulkSmsSemaphore.Release();
            }
        });

        // Block until every remaining task finishes
        await Task.WhenAll(tasks).ConfigureAwait(false);
        sw.Stop();
        var resultsList = results.ToList();
        Logger.LogInformation(
            "Bulk SMS send completed: {Success}/{Total} successful in {Elapsed}ms", resultsList.Count(r => r.IsSuccess), messageList.Count, sw.ElapsedMilliseconds);

        OnBulkSent(resultsList);
        var successCount = resultsList.Count(r => r.IsSuccess);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkSendTotal)], messageList.Count);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkSendSuccess)], successCount);
        Metrics.IncrementCounter(MetricNames[nameof(Constants.Metrics.BulkSendFailure)], messageList.Count - successCount);
        Metrics.RecordGauge(MetricNames[nameof(Constants.Metrics.BulkSendLastDurationMs)], sw.ElapsedMilliseconds);
        return resultsList;
    }

    /// <inheritdoc />
    public async Task<BulkResult<SmsRequest>> SendBulkAsync(BulkSmsBuilder bulkBuilder, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(bulkBuilder);
        var builders = bulkBuilder.Build().ToList();
        try {
            ArgumentHelpers.ThrowIfNullOrNotInRange(builders.Count, 1, Options.MaxBulkSmsLimit, nameof(bulkBuilder));
        }
        catch (ArgumentOutsideRangeException) {
            var error = $"Bulk request exceeds maximum limit of {Options.MaxBulkSmsLimit} messages. Requested: {builders.Count}";
            Logger.LogError(error);
            throw;
        }

        Logger.LogInformation("Starting bulk send for {Count} messages using BulkSmsBuilder", builders.Count);
        var sw = Stopwatch.StartNew();
        var results = await SendBulkAsync(builders, ct).ConfigureAwait(false);
        sw.Stop();
        return BulkResult<SmsRequest>.FromResults(results);
    }

    /// <summary>Builds a failed result for bulk work (for example a build failure or cancellation).</summary>
    protected abstract TResult CreateFailure(Exception exception, string code, SmsRequest? request = null);

    /// <summary>Builds the metric-name map. Derived types can override to supply their own names.</summary>
    protected virtual Dictionary<string, string> CreateMetricNamesDictionary()
        => new() {
            { nameof(Constants.Metrics.SendDuration), Constants.Metrics.SendDuration },
            { nameof(Constants.Metrics.SendSuccess), Constants.Metrics.SendSuccess },
            { nameof(Constants.Metrics.SendFailure), Constants.Metrics.SendFailure },
            { nameof(Constants.Metrics.BulkSendDuration), Constants.Metrics.BulkSendDuration },
            { nameof(Constants.Metrics.BulkSendTotal), Constants.Metrics.BulkSendTotal },
            { nameof(Constants.Metrics.BulkSendSuccess), Constants.Metrics.BulkSendSuccess },
            { nameof(Constants.Metrics.BulkSendFailure), Constants.Metrics.BulkSendFailure },
            { nameof(Constants.Metrics.BulkSendLastDurationMs), Constants.Metrics.BulkSendLastDurationMs }
        };

    /// <summary>Provider-specific connection probe; derived classes must implement this.</summary>
    protected abstract Task<bool> TestConnectionCoreAsync(CancellationToken ct = default);

    /// <summary>Fired just before a message is sent.</summary>
    public event EventHandler<SmsSendingEventArgs>? MessageSending;

    /// <summary>Fired after a message has been sent.</summary>
    public event EventHandler<SmsSentEventArgs>? MessageSent;

    /// <summary>Fired just before a bulk send begins.</summary>
    public event EventHandler<SmsBulkSendingEventArgs>? BulkSending;

    /// <summary>Fired after a bulk send finishes.</summary>
    public event EventHandler<BulkSmsSentEventArgs>? BulkSent;

    /// <summary>Provider-specific SMS or MMS send; derived classes must implement this.</summary>
    protected abstract Task<TResult> SendCoreAsync(SmsRequest request, CancellationToken ct = default);

    /// <summary>Default sender number.</summary>
    protected virtual string? GetDefaultFromPhoneNumber() => Options.DefaultFromPhoneNumber;

    /// <summary>Invokes MessageSending.</summary>
    private void OnMessageSending(SmsRequest request) => MessageSending?.Invoke(this, new(request));

    /// <summary>Invokes MessageSent.</summary>
    private void OnMessageSent(Result<SmsRequest> result) => MessageSent?.Invoke(this, new(result));

    /// <summary>Invokes BulkSending.</summary>
    private void OnBulkSending(IReadOnlyList<SmsRequest> messages) => BulkSending?.Invoke(this, new(messages));

    /// <summary>Invokes BulkSent.</summary>
    private void OnBulkSent(IReadOnlyList<TResult> results) => BulkSent?.Invoke(this, new(BulkResult<SmsRequest>.FromResults(results)));

    /// <summary>Frees unmanaged resources, and managed ones when <paramref name="disposing" /> is true.</summary>
    /// <param name="disposing">True to free managed and unmanaged resources; false for unmanaged only.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _disposedInt, 1, 0) != 0)
            return;

        if (disposing)
            BulkSmsSemaphore.Dispose();
    }
}