namespace Lyo.MessageQueue.RabbitMq.Web.Components;

/// <summary>Fields gathered by <see cref="RabbitMqCreateQueueDialog" /> before the workbench declares the queue.</summary>
public sealed class RabbitMqCreateQueueRequest
{
    /// <summary>Name of the queue to declare.</summary>
    public string Name { get; set; } = "";

    /// <summary>Whether the queue lives through a broker restart. Defaults to true.</summary>
    public bool Durable { get; set; } = true;

    /// <summary>Whether only this connection may use the queue.</summary>
    public bool Exclusive { get; set; }

    /// <summary>Whether an unused queue is deleted.</summary>
    public bool AutoDelete { get; set; }

    /// <summary>If true, declare through <c>CreateQueueWithDlq</c>.</summary>
    public bool CreateWithDlq { get; set; }

    /// <summary>Optional DLQ name. Empty becomes <c>{queue}.dlq</c>.</summary>
    public string DlqName { get; set; } = "";

    /// <summary>Broker <c>x-max-priority</c>. Zero skips the argument.</summary>
    public int MaxPriority { get; set; }
}
