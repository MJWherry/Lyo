namespace Lyo.MessageQueue;

public class MqServiceHealth
{
    public IReadOnlyList<MessageQueueInfo> Queues { get; set; } = [];

    public IReadOnlyList<ConnectionInfo> Connections { get; set; } = [];
}