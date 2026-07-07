using System.Text.Json;
using Lyo.Job.Models;
using Lyo.MessageQueue;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.SignalR;

/// <summary>Subscribes to <c>job.events</c> and pushes updates to <see cref="JobHub" /> clients.</summary>
public sealed class JobEventBroadcaster : BackgroundService
{
    private const string DashboardQueue = "job.signalr.dashboard";

    private static readonly string[] RoutingKeys = [
        Constants.Mq.JobRunCreatedRoutingKey,
        Constants.Mq.JobRunStartedRoutingKey,
        Constants.Mq.JobRunFinishedRoutingKey,
        Constants.Mq.JobRunCancelledRoutingKey,
        Constants.Mq.JobAlertRoutingKey,
        Constants.Mq.JobDefinitionChangeKey
    ];

    private readonly IHubContext<JobHub> _hub;
    private readonly ILogger<JobEventBroadcaster> _logger;
    private readonly IMqService _mqService;

    public JobEventBroadcaster(IHubContext<JobHub> hub, IMqService mqService, ILogger<JobEventBroadcaster>? logger = null)
    {
        _hub = hub;
        _mqService = mqService;
        _logger = logger ?? NullLogger<JobEventBroadcaster>.Instance;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_mqService.IsConnected())
            await _mqService.ConnectAsync(stoppingToken).ConfigureAwait(false);

        foreach (var routingKey in RoutingKeys) {
            var queue = $"{DashboardQueue}.{routingKey.Replace('.', '_')}";
            await _mqService.CreateQueue(queue, true, false, false, null, stoppingToken).ConfigureAwait(false);
            await _mqService.BindQueueToExchange(queue, Constants.Mq.JobEventExchange, routingKey, stoppingToken).ConfigureAwait(false);
            var eventType = MapEventType(routingKey);
            await _mqService.SubscribeToQueue(queue, body => OnEventAsync(body, eventType), stoppingToken).ConfigureAwait(false);
        }

        try {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // shutdown
        }
    }

    private async Task<bool> OnEventAsync(byte[] body, string eventType)
    {
        try {
            Guid? id = null;
            try {
                id = JsonSerializer.Deserialize<Guid>(body);
            }
            catch {
                // non-guid payloads (alerts) are still forwarded as text
            }

            var hubEvent = new JobHubEvent(
                eventType,
                eventType.StartsWith("job.notifications.run.", StringComparison.Ordinal) ? id : null,
                eventType == Constants.Mq.JobDefinitionChangeKey ? id : null,
                null,
                DateTime.UtcNow,
                id is null ? System.Text.Encoding.UTF8.GetString(body) : null);

            await _hub.Clients.All.SendAsync("JobEvent", hubEvent, CancellationToken.None).ConfigureAwait(false);
            return false;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to broadcast job event");
            return true;
        }
    }

    private static string MapEventType(string routingKey)
        => routingKey switch {
            Constants.Mq.JobRunCreatedRoutingKey => "run.created",
            Constants.Mq.JobRunStartedRoutingKey => "run.started",
            Constants.Mq.JobRunFinishedRoutingKey => "run.finished",
            Constants.Mq.JobRunCancelledRoutingKey => "run.cancelled",
            Constants.Mq.JobAlertRoutingKey => "alert",
            Constants.Mq.JobDefinitionChangeKey => "definition.updated",
            _ => routingKey
        };
}
