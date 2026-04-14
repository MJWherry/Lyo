namespace Lyo.MessageQueue.RabbitMq;

/// <summary>RabbitMQ-specific queue operations such as exchanges, bindings, and routed publish.</summary>
public interface IRabbitMqService : IMqService
{
    Task<bool> CreateExchange(
        string exchangeName,
        string exchangeType = "direct",
        bool durable = true,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default);

    Task<bool> DeleteExchange(string exchangeName, bool ifUnused = false, CancellationToken ct = default);

    Task<bool> BindQueueToExchange(string queueName, string exchangeName, string routingKey, CancellationToken ct = default);

    Task<bool> SendToExchange(string exchangeName, string routingKey, byte[] data);
}