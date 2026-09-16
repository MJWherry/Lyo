using System.Text;
using System.Text.Json;
using Lyo.Http.Client;
using Lyo.Common.Metadata.Records;
using Lyo.Job.Client;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Security;
using Lyo.Job.Worker;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Constants = Lyo.Job.Workers.Notification.Constants;
using Lyo.Exceptions;

namespace Lyo.Job.Workers.Notification;

/// <summary>
/// POSTs a run's <c>Body</c> to its <c>Url</c> as a workflow step. This replaces <c>JobDefinition.AlertWebhookUrl</c>: a webhook is now a step in a graph, which means it can
/// be conditional, can read upstream results through templated parameters, and — the part the column could not do — <b>fails the run when the send fails</b>, so job retry,
/// backoff, DLQ, and run-log history all apply.
/// <para>
/// Uses <see cref="ILyoHttpClient" /> rather than <c>Lyo.Webhook</c>: that package is inbound-only (signature verification and endpoint mapping) and has no sender.
/// </para>
/// </summary>
/// <remarks>
/// Parameters: <c>Url</c> (required), <c>Body</c> (required, sent verbatim), <c>Headers</c> (optional JSON object of header name to value). Results: <c>StatusCode</c> and
/// <c>Attempts</c>, so a downstream step can condition on the outcome.
/// </remarks>
public class WebhookNotificationWorker : JobWorkerBase
{
    private readonly ILyoHttpClient _http;
    private readonly WebhookNotificationWorkerOptions _options;

    public WebhookNotificationWorker(
        IMqService mq,
        IJobClient jobClient,
        IJobEventPublisher events,
        ILyoHttpClient http,
        WebhookNotificationWorkerOptions options,
        ILogger<WebhookNotificationWorker>? logger = null,
        IMetrics? metrics = null,
        IJobParameterEncryptionService? parameterEncryption = null)
        : base(mq, jobClient, events, options.WorkerType, logger, metrics, options.MaxRequeueCount, options.DlqName, parameterEncryption)
    {
        ArgumentHelpers.ThrowIfNull(http);
        ArgumentHelpers.ThrowIfNull(options);
        options.Validate();
        _http = http;
        _options = options;
    }

    protected override async Task ExecuteAsync(IJobWorkerContext ctx)
    {
        if (!NotificationParameters.TryRequired(ctx, Constants.ParameterKey.Url, out var url)
            || !NotificationParameters.TryRequired(ctx, Constants.ParameterKey.Body, out var body))
            return;

        var headers = ParseHeaders(ctx);
        var contentType = NotificationParameters.Optional(ctx, Constants.ParameterKey.ContentType) ?? Constants.DefaultContentType;
        var attempts = _options.RetryCount + 1;
        for (var attempt = 1; attempt <= attempts; attempt++) {
            try {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ctx.CancellationToken);
                timeoutCts.CancelAfter(_options.Timeout);
                await _http.PostAsBinaryAsync<object?>(url, null, request => BuildRequest(request, body, contentType, headers), timeoutCts.Token).ConfigureAwait(false);
                ctx.Results.AddResult(Constants.ResultKey.StatusCode, 200, LyoTypeInfo.Int);
                ctx.Results.AddCount(Constants.ResultKey.Attempts, attempt);
                return;
            }
            catch (LyoHttpException ex) when (IsRetryable(ex.StatusCode) && attempt < attempts) {
                ctx.Logger.LogWarning("Webhook attempt {Attempt}/{Attempts} to {Url} returned {StatusCode}", attempt, attempts, url, ex.StatusCode);
            }
            catch (LyoHttpException ex) {
                // Either a permanent status or the last attempt. Both are the end of the road, and the run has to show it.
                ctx.Results.AddResult(Constants.ResultKey.StatusCode, ex.StatusCode, LyoTypeInfo.Int);
                ctx.Results.AddCount(Constants.ResultKey.Attempts, attempt);
                ctx.Results.AddError($"Webhook POST to '{url}' returned {ex.StatusCode} after {attempt} attempt(s).");
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ctx.CancellationToken.IsCancellationRequested) {
                if (attempt == attempts) {
                    ctx.Results.AddCount(Constants.ResultKey.Attempts, attempt);
                    ctx.Results.AddError($"Webhook POST to '{url}' failed after {attempt} attempt(s): {ex.Message}");
                    return;
                }

                ctx.Logger.LogDebug(ex, "Webhook attempt {Attempt}/{Attempts} to {Url} failed", attempt, attempts, url);
            }

            var delay = TimeSpan.FromTicks(_options.RetryDelay.Ticks * (1L << (attempt - 1)));
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ctx.CancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>5xx plus 408 and 429 are worth another try. Other 4xx will be rejected identically every time, so retrying only delays the failure.</summary>
    private static bool IsRetryable(int statusCode) => statusCode is 408 or 429 || statusCode >= 500;

    /// <summary>Reads the optional <c>Headers</c> parameter. A malformed value is logged and ignored rather than failing the send: the notification still has value without it.</summary>
    private static IReadOnlyDictionary<string, string> ParseHeaders(IJobWorkerContext ctx)
    {
        var raw = NotificationParameters.Optional(ctx, Constants.ParameterKey.Headers);
        if (raw is null)
            return new Dictionary<string, string>();

        try {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(raw) ?? [];
        }
        catch (JsonException ex) {
            ctx.Logger.LogWarning(ex, "Webhook run {RunId} has a malformed Headers parameter; sending without custom headers", ctx.Run.Id);
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Replaces the serialized body with the raw <c>Body</c> parameter. The client's own serializer would JSON-encode the string, so a payload of <c>{"a":1}</c> would arrive
    /// at the endpoint as the quoted literal <c>"{\"a\":1}"</c>. A webhook body has to go over the wire exactly as the step wrote it.
    /// </summary>
    private static void BuildRequest(HttpRequestMessage request, string body, string contentType, IReadOnlyDictionary<string, string> headers)
    {
        request.Content?.Dispose();
        request.Content = new StringContent(body, Encoding.UTF8, contentType);
        foreach (var header in headers)
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
    }
}
