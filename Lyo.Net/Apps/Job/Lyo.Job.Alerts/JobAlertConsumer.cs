using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Common.Core.Extensions;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Lyo.Notification;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Alerts;

/// <summary>
/// Listens on <c>job.events</c> with routing key <see cref="Constants.Mq.JobAlertRoutingKey" /> and sends deserialized <see cref="JobAlertEvent" /> payloads through
/// <see cref="INotificationPublisher" /> and/or HTTP POST to <see cref="JobAlertsOptions.AlertWebhookUrl" />.
/// </summary>
/// <remarks>
/// Repeat alerts for the same definition and alert type are held for <see cref="JobAlertsOptions.DedupWindow" />. Messages that fail to deserialize go to
/// <see cref="JobAlertsOptions.DeadLetterQueueName" /> instead of being acked and dropped.
/// </remarks>
public sealed class JobAlertConsumer : BackgroundService
{
    private const string AlertQueueName = "job.notifications.alert";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ConcurrentDictionary<string, DateTime> _lastDispatchUtc = new(StringComparer.Ordinal);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JobAlertConsumer> _logger;
    private readonly IMetrics _metrics;
    private readonly IMqService _mqService;
    private readonly INotificationPublisher? _notificationPublisher;
    private readonly JobAlertsOptions _options;

    public JobAlertConsumer(
        IMqService mqService,
        JobAlertsOptions options,
        ILogger<JobAlertConsumer> logger,
        IHttpClientFactory httpClientFactory,
        INotificationPublisher? notificationPublisher = null,
        IMetrics? metrics = null)
    {
        _mqService = mqService;
        _options = options;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _notificationPublisher = notificationPublisher;
        _metrics = metrics ?? NullMetrics.Instance;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_mqService.IsConnected())
            await _mqService.ConnectAsync(stoppingToken).ConfigureAwait(false);

        await _mqService.CreateQueue(AlertQueueName, true, false, false, null, stoppingToken).ConfigureAwait(false);
        await _mqService.BindQueueToExchange(AlertQueueName, Constants.Mq.JobEventExchange, Constants.Mq.JobAlertRoutingKey, stoppingToken).ConfigureAwait(false);
        if (!_options.DeadLetterQueueName.IsNullOrWhitespace())
            await _mqService.CreateQueue(_options.DeadLetterQueueName!, true, false, false, null, stoppingToken).ConfigureAwait(false);

        await _mqService.SubscribeToQueue(AlertQueueName, body => HandleMessageAsync(body, stoppingToken), stoppingToken).ConfigureAwait(false);
        try {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // Expected on shutdown
        }
    }

    private async Task<bool> HandleMessageAsync(byte[] body, CancellationToken ct)
    {
        JobAlertEvent? alert;
        try {
            alert = JsonSerializer.Deserialize<JobAlertEvent>(body, JsonOptions);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to deserialize job alert message ({Size} bytes)", body.Length);
            await RouteToDeadLetterAsync(body, "deserialization failed").ConfigureAwait(false);
            return false;
        }

        if (alert is null) {
            _logger.LogWarning("Job alert message deserialized to null ({Size} bytes)", body.Length);
            await RouteToDeadLetterAsync(body, "deserialized to null").ConfigureAwait(false);
            return false;
        }

        if (IsDuplicate(alert)) {
            _logger.LogDebug(
                "Suppressed duplicate {AlertType} alert for definition {DefinitionId} inside the {DedupWindow} dedup window", alert.AlertType, alert.DefinitionId,
                _options.DedupWindow);
            _metrics.IncrementCounter(Constants.Metrics.Alerts.Suppressed, tags: [("alert_type", alert.AlertType.ToString())]);
            return false;
        }

        try {
            if (_notificationPublisher is not null)
                await _notificationPublisher.PublishAsync(alert).ConfigureAwait(false);

            if (!_options.AlertWebhookUrl.IsNullOrWhitespace())
                await PostWebhookAsync(alert, ct).ConfigureAwait(false);

            _metrics.IncrementCounter(Constants.Metrics.Alerts.Dispatched, tags: [("alert_type", alert.AlertType.ToString())]);
            return false;
        }
        catch (Exception ex) {
            // Dedup already recorded this send, so allow redelivery instead of swallowing the retry.
            ForgetDedupEntry(alert);
            _logger.LogError(ex, "Failed to dispatch job alert for definition {DefinitionId}", alert.DefinitionId);
            _metrics.IncrementCounter(Constants.Metrics.Alerts.DispatchFailed, tags: [("alert_type", alert.AlertType.ToString())]);
            return true;
        }
    }

    /// <summary>
    /// True if the same definition/alert-type pair already went out inside <see cref="JobAlertsOptions.DedupWindow" />. Records the send when it is not a
    /// duplicate, so the caller must ask only once per message.
    /// </summary>
    private bool IsDuplicate(JobAlertEvent alert)
    {
        if (_options.DedupWindow <= TimeSpan.Zero)
            return false;

        var now = DateTime.UtcNow;
        var key = DedupKey(alert);
        var duplicate = false;
        _lastDispatchUtc.AddOrUpdate(
            key, now, (_, previous) => {
                if (now - previous >= _options.DedupWindow)
                    return now;

                duplicate = true;
                return previous;
            });

        if (!duplicate)
            PruneDedupCache(now);

        return duplicate;
    }

    private void ForgetDedupEntry(JobAlertEvent alert)
    {
        if (_options.DedupWindow > TimeSpan.Zero)
            _lastDispatchUtc.TryRemove(DedupKey(alert), out _);
    }

    private static string DedupKey(JobAlertEvent alert) => $"{alert.DefinitionId:N}:{(int)alert.AlertType}";

    /// <summary>Drops entries older than the dedup window, then the oldest leftovers, so a churn of definition ids cannot grow the cache without a cap.</summary>
    private void PruneDedupCache(DateTime now)
    {
        if (_lastDispatchUtc.Count <= _options.DedupCacheLimit)
            return;

        foreach (var entry in _lastDispatchUtc) {
            if (now - entry.Value >= _options.DedupWindow)
                _lastDispatchUtc.TryRemove(entry.Key, out _);
        }

        var excess = _lastDispatchUtc.Count - _options.DedupCacheLimit;
        if (excess <= 0)
            return;

        foreach (var entry in _lastDispatchUtc.OrderBy(e => e.Value).Take(excess))
            _lastDispatchUtc.TryRemove(entry.Key, out _);
    }

    /// <summary>Sends the original bytes to the configured DLQ so an unparseable alert can be inspected instead of disappearing. Never throws.</summary>
    private async Task RouteToDeadLetterAsync(byte[] body, string reason)
    {
        _metrics.IncrementCounter(Constants.Metrics.Alerts.Poison);
        if (_options.DeadLetterQueueName.IsNullOrWhitespace()) {
            _logger.LogWarning("Dropping poison job alert message ({Reason}) — no {Option} configured", reason, nameof(JobAlertsOptions.DeadLetterQueueName));
            return;
        }

        try {
            await _mqService.CreateQueue(_options.DeadLetterQueueName!).ConfigureAwait(false);
            await _mqService.SendToQueue(_options.DeadLetterQueueName!, body).ConfigureAwait(false);
            _logger.LogWarning("Routed poison job alert message ({Reason}) to {DlqName}", reason, _options.DeadLetterQueueName);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to route poison job alert message to {DlqName}", _options.DeadLetterQueueName);
            _metrics.IncrementCounter(Constants.Metrics.Alerts.PoisonDlqFailed);
        }
    }

    /// <summary>
    /// POSTs the alert to the webhook. Transient failures retry up to <see cref="JobAlertsOptions.WebhookRetryCount" /> extra times with exponential backoff. Throws only
    /// when every attempt failed, which puts the message back on the queue for a later delivery.
    /// </summary>
    private async Task PostWebhookAsync(JobAlertEvent alert, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(nameof(JobAlertConsumer));
        var attempts = _options.WebhookRetryCount + 1;
        for (var attempt = 1; attempt <= attempts; attempt++) {
            try {
                using var response = await client.PostAsJsonAsync(_options.AlertWebhookUrl!, alert, JsonOptions, ct).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    return;

                if (!IsRetryable(response.StatusCode) || attempt == attempts) {
                    _logger.LogWarning(
                        "Job alert webhook returned {StatusCode} for definition {DefinitionId} after {Attempts} attempt(s)", response.StatusCode, alert.DefinitionId, attempt);
                    _metrics.IncrementCounter(Constants.Metrics.Alerts.WebhookFailed, tags: [("status", ((int)response.StatusCode).ToString())]);
                    return;
                }

                _metrics.IncrementCounter(Constants.Metrics.Alerts.WebhookAttemptFailed, tags: [("status", ((int)response.StatusCode).ToString())]);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested) {
                if (attempt == attempts) {
                    _metrics.IncrementCounter(Constants.Metrics.Alerts.WebhookFailed, tags: [("status", "exception")]);
                    throw;
                }

                _logger.LogDebug(ex, "Job alert webhook attempt {Attempt}/{Attempts} failed for definition {DefinitionId}", attempt, attempts, alert.DefinitionId);
                _metrics.IncrementCounter(Constants.Metrics.Alerts.WebhookAttemptFailed, tags: [("status", "exception")]);
            }

            var delay = TimeSpan.FromTicks(_options.WebhookRetryDelay.Ticks * (1L << (attempt - 1)));
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ct).ConfigureAwait(false);
        }
    }

    /// <summary>5xx and throttle responses are worth another try. Other 4xx (except 408/429) will fail the same way every time.</summary>
    private static bool IsRetryable(HttpStatusCode status)
        => status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)status >= 500;
}
