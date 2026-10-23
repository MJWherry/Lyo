using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Authentication;
using Lyo.Email.Builders;
using Lyo.Email.Models;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Email;

/// <summary>Shared send pipeline. Providers implement the wire call. This type applies default From, limits, metrics, logging, and events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class EmailServiceBase : IEmailService
{
    private readonly Dictionary<string, string> _metricNames;

    /// <summary>Constructs the shared pipeline.</summary>
    /// <param name="options">Shared or provider options. Metrics stay off unless <see cref="EmailServiceOptions.EnableMetrics" /> is true and <paramref name="metrics" /> is set.</param>
    /// <param name="logger">Logger. A null logger is used when this is omitted.</param>
    /// <param name="metrics">Metrics sink. Ignored when metrics are disabled on <paramref name="options" />.</param>
    protected EmailServiceBase(EmailServiceOptions options, ILogger? logger = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        Options = options;
        Logger = logger ?? NullLogger.Instance;
        Metrics = options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _metricNames = new() {
            { nameof(Constants.Metrics.SendDuration), Constants.Metrics.SendDuration },
            { nameof(Constants.Metrics.SendSuccess), Constants.Metrics.SendSuccess },
            { nameof(Constants.Metrics.SendFailure), Constants.Metrics.SendFailure },
            { nameof(Constants.Metrics.SendCancelled), Constants.Metrics.SendCancelled },
            { nameof(Constants.Metrics.SendLastDurationMs), Constants.Metrics.SendLastDurationMs },
            { nameof(Constants.Metrics.BulkSendDuration), Constants.Metrics.BulkSendDuration },
            { nameof(Constants.Metrics.BulkSendTotal), Constants.Metrics.BulkSendTotal },
            { nameof(Constants.Metrics.BulkSendSuccess), Constants.Metrics.BulkSendSuccess },
            { nameof(Constants.Metrics.BulkSendFailure), Constants.Metrics.BulkSendFailure },
            { nameof(Constants.Metrics.BulkSendLastDurationMs), Constants.Metrics.BulkSendLastDurationMs },
            { nameof(Constants.Metrics.TestConnectionDuration), Constants.Metrics.TestConnectionDuration },
            { nameof(Constants.Metrics.TestConnectionSuccess), Constants.Metrics.TestConnectionSuccess },
            { nameof(Constants.Metrics.TestConnectionFailure), Constants.Metrics.TestConnectionFailure }
        };
    }

    /// <summary>Raised just before a send begins.</summary>
    public event EventHandler<EmailSendingEventArgs>? EmailSending;

    /// <summary>Raised after a send finishes, whether it succeeded or failed.</summary>
    public event EventHandler<EmailSentEventArgs>? EmailSent;

    /// <summary>Raised just before a bulk send begins.</summary>
    public event EventHandler<EmailBulkSendingEventArgs>? BulkSending;

    /// <summary>Raised after a bulk send finishes.</summary>
    public event EventHandler<BulkEmailSentEventArgs>? BulkEmailSent;

    /// <summary>Raised after a connection probe finishes.</summary>
    public event EventHandler<ConnectionTestedEventArgs>? ConnectionTested;

    /// <summary>Options this service was constructed with.</summary>
    protected EmailServiceOptions Options { get; }

    /// <summary>Logger used by this service.</summary>
    protected ILogger Logger { get; }

    /// <summary>Metrics sink. <see cref="NullMetrics" /> when metrics are off.</summary>
    protected IMetrics Metrics { get; }

    /// <inheritdoc />
    public Task<EmailResult> SendEmailAsync(EmailRequestBuilder requestBuilder, string fromAddress, string? fromName = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(requestBuilder);
        var request = requestBuilder.Build();
        if (!string.IsNullOrWhiteSpace(fromAddress))
            request = request with { FromAddress = fromAddress, FromName = fromName ?? fromAddress };

        return SendPreparedAsync(request, ct);
    }

    /// <inheritdoc />
    public Task<EmailResult> SendEmailAsync(EmailRequestBuilder requestBuilder, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(requestBuilder);
        var request = requestBuilder.Build();
        if (string.IsNullOrWhiteSpace(request.FromAddress))
            request = request with { FromAddress = Options.DefaultFromAddress, FromName = Options.DefaultFromName };

        return SendPreparedAsync(request, ct);
    }

    /// <inheritdoc />
    public Task<EmailResult> SendEmailAsync(EmailRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return SendPreparedAsync(request, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmailResult>> SendBulkEmailAsync(IEnumerable<EmailRequestBuilder> builders, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(builders);
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.BulkSendDuration)]);
        var sw = Stopwatch.StartNew();
        var builderList = builders as IReadOnlyList<EmailRequestBuilder> ?? builders.ToList();
        ArgumentHelpers.ThrowIfNotInRange(builderList.Count, 0, Options.MaxBulkEmailLimit, nameof(builders));
        if (builderList.Count == 0) {
            FinishBulk(sw, 0, []);
            return [];
        }

        var buildFailures = new List<EmailResult>();
        var prepared = new List<EmailRequest>(builderList.Count);
        foreach (var builder in builderList) {
            try {
                var request = PrepareBuilderRequest(builder);
                EnforceAttachmentLimit(request);
                prepared.Add(request);
            }
            catch (ArgumentOutsideRangeException) {
                throw;
            }
            catch (Exception ex) {
                buildFailures.Add(EmailResult.FromException(ex, new(ToAddresses: [new EmailAddress("unknown")], Subject: "Failed to build"), EmailErrorCodes.BuildFailed));
            }
        }

        if (prepared.Count > 0)
            BulkSending?.Invoke(this, new(prepared));

        var sent = prepared.Count == 0 ? [] : await SendBulkCoreAsync(prepared, ct).ConfigureAwait(false);
        foreach (var result in sent)
            RecordSend(result);

        var results = buildFailures.Concat(sent).ToArray();
        FinishBulk(sw, builderList.Count, results);
        return results;
    }

    /// <inheritdoc />
    public async Task<BulkResult<EmailRequest>> SendBulkEmailAsync(BulkEmailRequestBuilder bulkRequestBuilder, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(bulkRequestBuilder);
        var results = await SendBulkEmailAsync(bulkRequestBuilder.Build(), ct).ConfigureAwait(false);
        return BulkResult<EmailRequest>.FromResults(results);
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.TestConnectionDuration)]);
        var sw = Stopwatch.StartNew();
        try {
            var ok = await TestConnectionCoreAsync(ct).ConfigureAwait(false);
            sw.Stop();
            if (ok) {
                Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.TestConnectionSuccess)]);
                ConnectionTested?.Invoke(this, new(true, sw.Elapsed));
                return true;
            }

            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.TestConnectionFailure)]);
            ConnectionTested?.Invoke(this, new(false, sw.Elapsed));
            return false;
        }
        catch (OperationCanceledException ex) {
            sw.Stop();
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.TestConnectionFailure)]);
            Metrics.RecordError(_metricNames[nameof(Constants.Metrics.TestConnectionDuration)], ex);
            ConnectionTested?.Invoke(this, new(false, sw.Elapsed, ex));
            throw;
        }
        catch (Exception ex) {
            sw.Stop();
            Logger.LogError(ex, "Email connection test failed");
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.TestConnectionFailure)]);
            Metrics.RecordError(_metricNames[nameof(Constants.Metrics.TestConnectionDuration)], ex);
            ConnectionTested?.Invoke(this, new(false, sw.Elapsed, ex));
            return false;
        }
    }

    /// <summary>Sends one prepared request. Implementations apply a default From on the wire when <see cref="EmailRequest.FromAddress" /> is null and leave that null on the returned request.</summary>
    /// <param name="request">Request to send.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>A successful result. Throw when the provider call fails. A non-success result is also accepted for providers that report failure without throwing.</returns>
    protected abstract Task<EmailResult> SendCoreAsync(EmailRequest request, CancellationToken ct);

    /// <summary>Probes the provider. Throw <see cref="OperationCanceledException" /> when <paramref name="ct" /> is cancelled. Throw or return false on any other failure.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the probe succeeded.</returns>
    protected abstract Task<bool> TestConnectionCoreAsync(CancellationToken ct);

    /// <summary>
    /// Sends the prepared requests. The base fans out up to <see cref="EmailServiceOptions.BulkEmailConcurrencyLimit" /> calls. SMTP overrides this and keeps one connection.
    /// </summary>
    /// <param name="requests">Requests that already have a From and are within the attachment cap.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>One result per request.</returns>
    protected virtual async Task<IReadOnlyList<EmailResult>> SendBulkCoreAsync(IReadOnlyList<EmailRequest> requests, CancellationToken ct)
    {
        var limit = Math.Max(1, Options.BulkEmailConcurrencyLimit);
        using var gate = new SemaphoreSlim(limit, limit);
        var results = new EmailResult[requests.Count];
        var tasks = new Task[requests.Count];
        for (var i = 0; i < requests.Count; i++) {
            var index = i;
            tasks[i] = SendOneAsync(index);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;

        async Task SendOneAsync(int index)
        {
            try {
                await gate.WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) {
                results[index] = EmailResult.FromException(ex, requests[index], EmailErrorCodes.OperationCancelled);
                return;
            }

            try {
                results[index] = await MapSendAsync(requests[index], ct).ConfigureAwait(false);
            }
            finally {
                gate.Release();
            }
        }
    }

    /// <summary>Maps a thrown provider failure onto one <see cref="EmailResult" />. Cancelled calls use <see cref="EmailErrorCodes.OperationCancelled" />. Every other exception uses <see cref="EmailErrorCodes.SendFailed" />.</summary>
    /// <param name="exception">Failure from the provider call.</param>
    /// <param name="request">Request that failed.</param>
    /// <returns>The failed result.</returns>
    protected EmailResult MapSendFailure(Exception exception, EmailRequest request)
    {
        if (exception is OperationCanceledException) {
            Logger.LogWarning(exception, "Email send was cancelled. Subject: {Subject}", request.Subject);
            return EmailResult.FromException(exception, request, EmailErrorCodes.OperationCancelled);
        }

        Logger.LogError(exception, "Failed to send email. Subject: {Subject}. {Detail}", request.Subject, Describe(exception));
        return EmailResult.FromException(exception, request, EmailErrorCodes.SendFailed);
    }

    private async Task<EmailResult> SendPreparedAsync(EmailRequest request, CancellationToken ct)
    {
        EnforceAttachmentLimit(request);
        using var timer = Metrics.StartTimer(_metricNames[nameof(Constants.Metrics.SendDuration)]);
        var sw = Stopwatch.StartNew();
        EmailSending?.Invoke(this, new(request));
        Logger.LogInformation("Sending email to {Recipients} with subject: {Subject}", Summarize(request), request.Subject);
        var result = await MapSendAsync(request, ct).ConfigureAwait(false);
        sw.Stop();
        RecordSend(result, sw.ElapsedMilliseconds);
        EmailSent?.Invoke(this, new(result));
        return result;
    }

    private async Task<EmailResult> MapSendAsync(EmailRequest request, CancellationToken ct)
    {
        try {
            return await SendCoreAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            return MapSendFailure(ex, request);
        }
    }

    private EmailRequest PrepareBuilderRequest(EmailRequestBuilder builder)
    {
        var request = builder.Build();
        if (string.IsNullOrWhiteSpace(request.FromAddress))
            request = request with { FromAddress = Options.DefaultFromAddress, FromName = Options.DefaultFromName };

        return request;
    }

    private void EnforceAttachmentLimit(EmailRequest request)
    {
        var count = request.Attachments?.Count ?? 0;
        ArgumentHelpers.ThrowIfNotInRange(
            count, 0, Options.MaxAttachmentCountPerEmail, nameof(request),
            $"Maximum of {Options.MaxAttachmentCountPerEmail} attachments allowed per email. Provided: {count}");
    }

    private void RecordSend(EmailResult result, long? elapsedMs = null)
    {
        if (result.IsSuccess)
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendSuccess)]);
        else if (result.Errors?.FirstOrDefault()?.Code == EmailErrorCodes.OperationCancelled)
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendCancelled)]);
        else
            Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.SendFailure)]);

        if (elapsedMs.HasValue)
            Metrics.RecordGauge(_metricNames[nameof(Constants.Metrics.SendLastDurationMs)], elapsedMs.Value);

        var exception = result.Errors?.FirstOrDefault()?.Exception;
        if (exception != null)
            Metrics.RecordError(_metricNames[nameof(Constants.Metrics.SendDuration)], exception);
    }

    private void FinishBulk(Stopwatch sw, int total, IReadOnlyList<EmailResult> results)
    {
        sw.Stop();
        var successCount = results.Count(static result => result.IsSuccess);
        var failureCount = total - successCount;
        Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.BulkSendTotal)], total);
        Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.BulkSendSuccess)], successCount);
        Metrics.IncrementCounter(_metricNames[nameof(Constants.Metrics.BulkSendFailure)], failureCount);
        Metrics.RecordGauge(_metricNames[nameof(Constants.Metrics.BulkSendLastDurationMs)], sw.ElapsedMilliseconds);
        BulkEmailSent?.Invoke(this, new(BulkResult<EmailRequest>.FromResults(results)));
        Logger.LogInformation("Bulk email send completed: {Success}/{Total} successful in {Elapsed}ms", successCount, total, sw.ElapsedMilliseconds);
    }

    private static string Describe(Exception exception)
        => exception switch {
            SocketException => "Unable to connect to the mail server. Check the network and provider settings.",
            TimeoutException => "The mail server connection timed out. The server may be busy or unreachable.",
            IOException => "An I/O error occurred while sending the email. Please try again.",
            AuthenticationException => "Mail authentication failed. Verify the username and password.",
            _ => exception.Message
        };

    private static string Summarize(EmailRequest request)
    {
        var parts = new List<string>();
        if (request.ToAddresses?.Count > 0)
            parts.Add($"{request.ToAddresses.Count} To");

        if (request.CcAddresses?.Count > 0)
            parts.Add($"{request.CcAddresses.Count} Cc");

        if (request.BccAddresses?.Count > 0)
            parts.Add($"{request.BccAddresses.Count} Bcc");

        return string.Join(", ", parts);
    }

    /// <summary>Service name and the options it was built with. Secrets stay out of the options text.</summary>
    public override string ToString() => $"{GetType().Name} {Options}";
}
