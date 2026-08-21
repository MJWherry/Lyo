namespace Lyo.MessageQueue.RabbitMq.Web.Components;

/// <summary>Values collected by <see cref="RabbitMqCreateQueueDialog" /> before the workbench declares the queue.</summary>
public sealed class RabbitMqCreateQueueRequest
{
    /// <summary>Queue name to declare.</summary>
    public string Name { get; set; } = "";

    /// <summary>Whether the queue survives a broker restart. Default true.</summary>
    public bool Durable { get; set; } = true;

    /// <summary>Whether the queue is exclusive to this connection.</summary>
    public bool Exclusive { get; set; }

    /// <summary>Whether the queue is deleted when unused.</summary>
    public bool AutoDelete { get; set; }

    /// <summary>When true, declare via <c>CreateQueueWithDlq</c>.</summary>
    public bool CreateWithDlq { get; set; }

    /// <summary>Optional DLQ name. Empty uses <c>{queue}.dlq</c>.</summary>
    public string DlqName { get; set; } = "";

    /// <summary>Broker <c>x-max-priority</c>. Zero omits the argument.</summary>
    public int MaxPriority { get; set; }
}
