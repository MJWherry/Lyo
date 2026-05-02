namespace Lyo.MessageQueue.RabbitMq;

/// <summary>RabbitMQ-specific queue operations such as exchange management in addition to the base <see cref="IMqService" /> contract.</summary>
public interface IRabbitMqService : IMqService
{
    /// <summary>Declares an exchange on the broker.</summary>
    Task<bool> CreateExchange(
        string exchangeName,
        string exchangeType = "direct",
        bool durable = true,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default);

    /// <summary>Deletes an exchange from the broker.</summary>
    Task<bool> DeleteExchange(string exchangeName, bool ifUnused = false, CancellationToken ct = default);
}