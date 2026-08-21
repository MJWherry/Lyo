namespace Lyo.MessageQueue.RabbitMq.Web.Components;

/// <summary>Values collected by <see cref="RabbitMqCreateExchangeDialog" /> before the workbench declares the exchange.</summary>
public sealed class RabbitMqCreateExchangeRequest
{
    /// <summary>Exchange name to declare.</summary>
    public string Name { get; set; } = "";

    /// <summary>direct, topic, fanout, or headers. Default direct.</summary>
    public string Type { get; set; } = "direct";

    /// <summary>Whether the exchange survives a broker restart. Default true.</summary>
    public bool Durable { get; set; } = true;

    /// <summary>Whether the exchange is deleted when unused.</summary>
    public bool AutoDelete { get; set; }
}
