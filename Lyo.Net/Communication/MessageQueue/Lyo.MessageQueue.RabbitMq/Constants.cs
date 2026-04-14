namespace Lyo.MessageQueue.RabbitMq;

/// <summary>Consolidated constants for the RabbitMQ message queue library.</summary>
public static class Constants
{
    /// <summary>Metric names and tags.</summary>
    public static class Metrics
    {
        public const string ConnectionEstablished = "mq.connection.established";
        public const string ConnectionFailed = "mq.connection.failed";
        public const string ConnectionClosed = "mq.connection.closed";
        public const string ConnectionLost = "mq.connection.lost";

        public const string QueueOperationDuration = "mq.queue.operation.duration";
        public const string QueueOperationDurationMs = "mq.queue.operation.duration_ms";
        public const string QueueOperationFailed = "mq.queue.operation.failed";
        public const string QueueCreated = "mq.queue.created";
        public const string QueueDeleted = "mq.queue.deleted";
        public const string QueueCleared = "mq.queue.cleared";
        public const string QueueSubscribed = "mq.queue.subscribed";
        public const string QueueSubscriptionFailed = "mq.queue.subscription_failed";

        public const string SendToQueueDuration = "mq.queue.send.duration";
        public const string SendToQueueSuccess = "mq.queue.send.success";
        public const string SendToQueueFailure = "mq.queue.send.failure";
        public const string SendToQueueMessageSizeBytes = "mq.queue.send.message_size_bytes";
        public const string SendToQueueDurationMs = "mq.queue.send.duration_ms";

        public const string SendToExchangeDuration = "mq.exchange.send.duration";
        public const string SendToExchangeSuccess = "mq.exchange.send.success";
        public const string SendToExchangeFailure = "mq.exchange.send.failure";
        public const string SendToExchangeMessageSizeBytes = "mq.exchange.send.message_size_bytes";
        public const string SendToExchangeDurationMs = "mq.exchange.send.duration_ms";

        public const string MessageProcessingDuration = "mq.message.processing.duration";
        public const string MessageProcessingDurationMs = "mq.message.processing.duration_ms";
        public const string MessageProcessingFailed = "mq.message.processing.failed";
        public const string MessageProcessed = "mq.message.processed";
        public const string MessageRequeued = "mq.message.requeued";

        public static class Tags
        {
            public const string Queue = "queue";
            public const string Exchange = "exchange";
            public const string RoutingKey = "routing_key";
            public const string Reason = "reason";
            public const string Operation = "operation";
        }
    }
}