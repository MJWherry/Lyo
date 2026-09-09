using System.Net;
using System.Text;
using System.Text.Json;
using Lyo.Health;
using Lyo.Job.Alerts;
using Lyo.Job.Models.Enums;
using Lyo.MessageQueue;
using Lyo.Notification;
using Microsoft.Extensions.Logging.Abstractions;
namespace Lyo.Job.Tests;

/// <summary>
/// The alert consumer had no coverage: a retry storm fanned every redelivery out to notification channels, and a message that would not deserialize was acked and lost. These
/// cover the dedup window, DLQ routing, and the webhook retry policy.
/// </summary>
public class JobAlertConsumerTests
{
    [Fact]
    public async Task RepeatAlert_InsideTheDedupWindow_IsSuppressed()
    {
        var (handler, publisher, _) = await StartConsumerAsync(new() { DedupWindow = TimeSpan.FromMinutes(5) });
        var alert = Alert(JobAlertType.Failure);

        Assert.False(await handler(Serialize(alert)));
        Assert.False(await handler(Serialize(alert)));
        Assert.Single(publisher.Published);
    }

    [Fact]
    public async Task RepeatAlert_ForADifferentAlertType_IsNotSuppressed()
    {
        var (handler, publisher, _) = await StartConsumerAsync(new() { DedupWindow = TimeSpan.FromMinutes(5) });
        var definitionId = Guid.NewGuid();

        await handler(Serialize(Alert(JobAlertType.Failure, definitionId)));
        await handler(Serialize(Alert(JobAlertType.SlaBreach, definitionId)));
        Assert.Equal(2, publisher.Published.Count);
    }

    [Fact]
    public async Task RepeatAlert_AfterTheWindowElapses_DispatchesAgain()
    {
        var (handler, publisher, _) = await StartConsumerAsync(new() { DedupWindow = TimeSpan.FromMilliseconds(50) });
        var alert = Alert(JobAlertType.Failure);

        await handler(Serialize(alert));
        await Task.Delay(TimeSpan.FromMilliseconds(120), TestContext.Current.CancellationToken);
        await handler(Serialize(alert));
        Assert.Equal(2, publisher.Published.Count);
    }

    [Fact]
    public async Task DedupDisabled_DispatchesEveryDelivery()
    {
        var (handler, publisher, _) = await StartConsumerAsync(new() { DedupWindow = TimeSpan.Zero });
        var alert = Alert(JobAlertType.Failure);

        await handler(Serialize(alert));
        await handler(Serialize(alert));
        Assert.Equal(2, publisher.Published.Count);
    }

    [Fact]
    public async Task UndeserializableMessage_IsRoutedToTheDeadLetterQueue()
    {
        var (handler, publisher, mq) = await StartConsumerAsync(new() { DeadLetterQueueName = "job.alerts.dlq" });
        var body = Encoding.UTF8.GetBytes("{ not json");

        // Acked (false) so it stops cycling, but preserved on the DLQ instead of vanishing.
        Assert.False(await handler(body));
        Assert.Empty(publisher.Published);
        var sent = Assert.Single(mq.Sends);
        Assert.Equal("job.alerts.dlq", sent.QueueName);
        Assert.Equal(body, sent.Data);
    }

    [Fact]
    public async Task UndeserializableMessage_WithoutADlq_IsDroppedWithoutThrowing()
    {
        var (handler, _, mq) = await StartConsumerAsync(new() { DeadLetterQueueName = null });
        Assert.False(await handler(Encoding.UTF8.GetBytes("{ not json")));
        Assert.Empty(mq.Sends);
    }

    [Fact]
    public async Task WebhookServerError_IsRetriedUpToTheConfiguredCount()
    {
        var webhook = new RecordingWebhookHandler(HttpStatusCode.InternalServerError);
        var (handler, _, _) = await StartConsumerAsync(
            new() {
                DedupWindow = TimeSpan.Zero,
                AlertWebhookUrl = "http://localhost/hook",
                WebhookRetryCount = 2,
                WebhookRetryDelay = TimeSpan.Zero
            }, webhook);

        await handler(Serialize(Alert(JobAlertType.Failure)));
        Assert.Equal(3, webhook.Requests);
    }

    [Fact]
    public async Task WebhookClientError_IsNotRetried()
    {
        var webhook = new RecordingWebhookHandler(HttpStatusCode.BadRequest);
        var (handler, _, _) = await StartConsumerAsync(
            new() { DedupWindow = TimeSpan.Zero, AlertWebhookUrl = "http://localhost/hook", WebhookRetryCount = 3, WebhookRetryDelay = TimeSpan.Zero }, webhook);

        await handler(Serialize(Alert(JobAlertType.Failure)));
        Assert.Equal(1, webhook.Requests);
    }

    [Fact]
    public async Task WebhookExhaustion_RequeuesAndLetsTheRetryDispatch()
    {
        var webhook = new RecordingWebhookHandler(new HttpRequestException("Connection refused"));
        var (handler, publisher, _) = await StartConsumerAsync(
            new() {
                DedupWindow = TimeSpan.FromMinutes(5),
                AlertWebhookUrl = "http://localhost/hook",
                WebhookRetryCount = 0,
                WebhookRetryDelay = TimeSpan.Zero
            }, webhook);

        var alert = Alert(JobAlertType.Failure);
        Assert.True(await handler(Serialize(alert)));

        // The dedup entry was recorded before the dispatch failed; forgetting it is what stops the requeued delivery from being suppressed.
        webhook.Succeed();
        Assert.False(await handler(Serialize(alert)));
        Assert.Equal(2, publisher.Published.Count);
    }

    private static byte[] Serialize(JobAlertEvent alert) => JsonSerializer.SerializeToUtf8Bytes(alert);

    private static JobAlertEvent Alert(JobAlertType type, Guid? definitionId = null)
        => new(definitionId ?? Guid.NewGuid(), Guid.NewGuid(), type, $"{type} alert", DateTime.UtcNow);

    /// <summary>
    /// Starts the consumer and captures the queue handler it subscribes with, which is the only way in to its private message pipeline. The consumer is left running: its
    /// stopping token is the one the webhook POST and its retry delays observe, so cancelling it would make every dispatch fail as cancelled.
    /// </summary>
    private static async Task<(Func<byte[], Task<bool>> Handler, RecordingNotificationPublisher Publisher, RecordingAlertMqService Mq)> StartConsumerAsync(
        JobAlertsOptions options,
        RecordingWebhookHandler? webhook = null)
    {
        var mq = new RecordingAlertMqService();
        var publisher = new RecordingNotificationPublisher();
        var consumer = new JobAlertConsumer(
            mq, options, NullLogger<JobAlertConsumer>.Instance, new SingleClientHttpClientFactory(webhook ?? new(HttpStatusCode.OK)), publisher);

        await consumer.StartAsync(CancellationToken.None);
        for (var i = 0; i < 200 && mq.Handler is null; i++)
            await Task.Delay(10, CancellationToken.None);

        Assert.NotNull(mq.Handler);
        return (mq.Handler!, publisher, mq);
    }

    private sealed class RecordingNotificationPublisher : INotificationPublisher
    {
        public List<INotification> Published { get; } = [];

        public Task PublishAsync<TNotification>(TNotification notification, CancellationToken ct = default)
            where TNotification : INotification
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class SingleClientHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false);
    }

    private sealed class RecordingWebhookHandler : HttpMessageHandler
    {
        private Exception? _exception;
        private HttpStatusCode _status;

        public RecordingWebhookHandler(HttpStatusCode status) => _status = status;

        public RecordingWebhookHandler(Exception exception)
        {
            _exception = exception;
            _status = HttpStatusCode.OK;
        }

        public int Requests { get; private set; }

        public void Succeed()
        {
            _exception = null;
            _status = HttpStatusCode.OK;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            if (_exception is not null)
                throw _exception;

            return Task.FromResult(new HttpResponseMessage(_status));
        }
    }

    /// <summary>Captures the queue handler the consumer subscribes with, and records DLQ sends.</summary>
    private sealed class RecordingAlertMqService : IMqService
    {
        public Func<byte[], Task<bool>>? Handler { get; private set; }

        public List<(string QueueName, byte[] Data)> Sends { get; } = [];

        public bool IsConnected() => true;

        public Task ConnectAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> CreateExchange(
            string exchangeName,
            string exchangeType = "direct",
            bool durable = true,
            bool autoDelete = false,
            IDictionary<string, object>? arguments = null,
            CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<bool> CreateQueue(
            string queueName,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            IDictionary<string, object>? arguments = null,
            CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<bool> DeleteQueue(string queueName, bool ifUnused = false, bool ifEmpty = false, CancellationToken ct = default) => Task.FromResult(true);

        public Task<bool> ClearQueue(string queueName, CancellationToken ct = default) => Task.FromResult(true);

        public Task<bool> BindQueueToExchange(string queueName, string exchangeName, string routingKey, CancellationToken ct = default) => Task.FromResult(true);

        public Task<bool> SendToQueue(string queueName, byte[] data)
        {
            Sends.Add((queueName, data));
            return Task.FromResult(true);
        }

        public Task<bool> SendToExchange(string exchangeName, string routingKey, byte[] data) => Task.FromResult(true);

        public Task<IReadOnlyList<QueuePeekMessage>> PeekQueueMessages(string queueName, int maxMessages = 10, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<QueuePeekMessage>>([]);

        public Task<bool> SubscribeToQueue(string queueName, Func<byte[], Task<bool>> onMessage, CancellationToken ct = default)
        {
            Handler = onMessage;
            return Task.FromResult(true);
        }

        public string HealthCheckName => "recording-alert-mq";

        public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => Task.FromResult(HealthResult.Healthy(TimeSpan.Zero, null, new Dictionary<string, object?>()));
    }
}
