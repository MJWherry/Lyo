using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Sms.Builders;
using Lyo.Sms.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Sms;

/// <summary>Abstract base class for SMS service implementations that provides common bulk SMS functionality.</summary>
/// <typeparam name="TResult">The type of result returned by send operations (e.g. <see cref="Result{SmsRequest}" /> or a provider-specific subtype).</typeparam>
public abstract class SmsServiceBase<TResult> : ISmsService<TResult>, IDisposable
    where TResult : Result<SmsRequest>
{
    private int _disposedInt;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger Logger { get; }

    /// <summary>Gets the SMS service options.</summary>
    protected SmsServiceOptions Options { get; }

    /// <summary>Gets the metrics instance (null if metrics are disabled).</summary>
    protected IMetrics Metrics { get; }

    /// <summary>Gets the metric names dictionary. Derived classes can modify this dictionary to override metric names.</summary>
    protected Dictionary<string, string> MetricNames { get; }

    /// <summary>Gets the semaphore for rate limiting bulk SMS operations.</summary>
    protected SemaphoreSlim BulkSmsSemaphore { get; }

    /// <summary>Initializes a new instance of the <see cref="SmsServiceBase{TResult}" /> class.</summary>
    /// <param name="options">The SMS service options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics instance for tracking SMS operations.</param>
    protected SmsServiceBase(SmsServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        Options = options;
        Logger = logger ?? NullLogger.Instance;
        Metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        BulkSmsSemaphore = new(options.BulkSmsConcurrencyLimit, options.BulkSmsConcurrencyLimit);
        MetricNames = CreateMetricNamesDictionary();
    }

    /// <summary>Releases the unmanaged resources used by the SmsServiceBase and optionally releases the managed resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async Task<TResult> SendAsync(SmsRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request, nameof(request));
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to, nameof(to));
        ArgumentHelpers.ThrowIfNull(body, nameof(body));
        var builder = SmsMessageBuilder.New().SetTo(to).SetBody(body);
        if (!string.IsNullOrWhiteSpace(from))
            builder.SetFrom(from!);
        else {
            var defaultFrom = Options.DefaultFromPhoneNumber;
            if (!string.IsNullOrWhiteSpace(defaultFrom))
                builder.SetFrom(defaultFrom!);
        }

        return SendAsync(builder, null, ct);
    }

    /// <inheritdoc />
    public Task<TResult> SendAsync(SmsMessageBuilder builder, string? from = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(builder, nameof(builder));
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(to, nameof(to));
        var mediaUrlList = mediaUrls.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(mediaUrlList, nameof(mediaUrls));
        var uris = mediaUrlList.Select(url => UriHelpers.GetValidUri(url, nameof(url))).ToList();
        return SendMmsAsync(to, uris, body, from, ct);
    }

    /// <inheritdoc />
    public Task<TResult> SendMmsAsync(string to, IEnumerable<Uri> mediaUrls, string? body = null, string? from = null, CancellationToken ct = default)
    {
        var mediaUrlList = mediaUrls.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(mediaUrlList, nameof(mediaUrls));
        var builder = SmsMessageBuilder.New().SetTo(to);
        if (!string.IsNullOrWhiteSpace(body))
            builder.SetBody(body);

        if (!string.IsNullOrWhiteSpace(from))
            builder.SetFrom(from!);
        else {
            var defaultFrom = GetDefaultFromPhoneNumber();
            if (!string.IsNullOrWhiteSpace(defaultFrom))
                builder.SetFrom(defaultFrom!);
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

        // Wait for all tasks to complete
        await Task.WhenAll(tasks).ConfigureAwait(false);
        sw.Stop();
        var resultsList = results.ToList();
        var successCount = resultsList.Count(r => r.IsSuccess);
        Logger.LogInformation("Bulk send completed: {Success}/{Total} successful in {Elapsed}ms", successCount, builderList.Count, sw.ElapsedMilliseconds);
        OnBulkSent(resultsList, sw.Elapsed);
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

        // Wait for all tasks to complete
        await Task.WhenAll(tasks).ConfigureAwait(false);
        sw.Stop();
        var resultsList = results.ToList();
        Logger.LogInformation(
            "Bulk SMS send completed: {Success}/{Total} successful in {Elapsed}ms", resultsList.Count(r => r.IsSuccess), messageList.Count, sw.ElapsedMilliseconds);

        OnBulkSent(resultsList, sw.Elapsed);
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
        ArgumentHelpers.ThrowIfNull(bulkBuilder, nameof(bulkBuilder));
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

    /// <summary>Creates a failed result for use in bulk operations (e.g. build failure, cancellation).</summary>
    protected abstract TResult CreateFailure(Exception exception, string code, SmsRequest? request = null);

    /// <summary>Creates the metric names dictionary. Override in derived classes to customize metric names.</summary>
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

    /// <summary>Tests the connection to the SMS service provider (must be implemented by derived classes).</summary>
    protected abstract Task<bool> TestConnectionCoreAsync(CancellationToken ct = default);

    /// <summary>Occurs before a message is sent.</summary>
    public event EventHandler<SmsSendingEventArgs>? MessageSending;

    /// <summary>Occurs after a message has been sent.</summary>
    public event EventHandler<SmsSentEventArgs>? MessageSent;

    /// <summary>Occurs before a bulk send operation starts.</summary>
    public event EventHandler<SmsBulkSendingEventArgs>? BulkSending;

    /// <summary>Occurs after a bulk send operation completes.</summary>
    public event EventHandler<BulkSmsSentEventArgs>? BulkSent;

    /// <summary>Sends a message (SMS or MMS) (must be implemented by derived classes).</summary>
    protected abstract Task<TResult> SendCoreAsync(SmsRequest request, CancellationToken ct = default);

    /// <summary>Gets the default sender phone number.</summary>
    protected virtual string? GetDefaultFromPhoneNumber() => Options.DefaultFromPhoneNumber;

    /// <summary>Raises the MessageSending event.</summary>
    private void OnMessageSending(SmsRequest request) => MessageSending?.Invoke(this, new(request));

    /// <summary>Raises the MessageSent event.</summary>
    private void OnMessageSent(Result<SmsRequest> result) => MessageSent?.Invoke(this, new(result));

    /// <summary>Raises the BulkSending event.</summary>
    private void OnBulkSending(IReadOnlyList<SmsRequest> messages) => BulkSending?.Invoke(this, new(messages));

    /// <summary>Raises the BulkSent event.</summary>
    private void OnBulkSent(IReadOnlyList<TResult> results, TimeSpan elapsedTime) => BulkSent?.Invoke(this, new(BulkResult<SmsRequest>.FromResults(results)));

    /// <summary>Releases the unmanaged resources used by the SmsServiceBase and optionally releases the managed resources.</summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _disposedInt, 1, 0) != 0)
            return;

        if (disposing)
            BulkSmsSemaphore.Dispose();
    }
}