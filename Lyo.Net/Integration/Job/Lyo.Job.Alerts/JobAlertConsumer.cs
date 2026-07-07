using System.Net.Http.Json;
using System.Text.Json;
using Lyo.Job.Models;
using Lyo.MessageQueue;
using Lyo.Notification;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Alerts;

/// <summary>
/// Subscribes to <c>job.events</c> with routing key <see cref="Constants.Mq.JobAlertRoutingKey" /> and dispatches deserialized
/// <see cref="JobAlertEvent" /> payloads via <see cref="INotificationPublisher" /> and/or HTTP POST to <see cref="JobAlertsOptions.AlertWebhookUrl" />.
/// </summary>
public sealed class JobAlertConsumer : BackgroundService
{
    private const string AlertQueueName = "job.notifications.alert";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JobAlertConsumer> _logger;
    private readonly IMqService _mqService;
    private readonly INotificationPublisher? _notificationPublisher;
    private readonly JobAlertsOptions _options;

    public JobAlertConsumer(
        IMqService mqService,
        IOptions<JobAlertsOptions> options,
        ILogger<JobAlertConsumer> logger,
        IHttpClientFactory httpClientFactory,
        INotificationPublisher? notificationPublisher = null)
    {
        _mqService = mqService;
        _options = options.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _notificationPublisher = notificationPublisher;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_mqService.IsConnected())
            await _mqService.ConnectAsync(stoppingToken).ConfigureAwait(false);

        await _mqService.CreateQueue(AlertQueueName, true, false, false, null, stoppingToken).ConfigureAwait(false);
        await _mqService.BindQueueToExchange(AlertQueueName, Constants.Mq.JobEventExchange, Constants.Mq.JobAlertRoutingKey, stoppingToken).ConfigureAwait(false);
        await _mqService.SubscribeToQueue(AlertQueueName, HandleMessageAsync, stoppingToken).ConfigureAwait(false);

        try {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // Normal shutdown
        }
    }

    private async Task<bool> HandleMessageAsync(byte[] body)
    {
        JobAlertEvent? alert;
        try {
            alert = JsonSerializer.Deserialize<JobAlertEvent>(body, JsonOptions);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to deserialize job alert message");
            return false;
        }

        if (alert is null) {
            _logger.LogWarning("Job alert message deserialized to null");
            return false;
        }

        try {
            if (_notificationPublisher is not null)
                await _notificationPublisher.PublishAsync(alert).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(_options.AlertWebhookUrl))
                await PostWebhookAsync(alert).ConfigureAwait(false);

            return false;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to dispatch job alert for definition {DefinitionId}", alert.DefinitionId);
            return true;
        }
    }

    private async Task PostWebhookAsync(JobAlertEvent alert)
    {
        var client = _httpClientFactory.CreateClient(nameof(JobAlertConsumer));
        using var response = await client.PostAsJsonAsync(_options.AlertWebhookUrl!, alert, JsonOptions).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Job alert webhook returned {StatusCode} for definition {DefinitionId}", response.StatusCode, alert.DefinitionId);
    }
}
