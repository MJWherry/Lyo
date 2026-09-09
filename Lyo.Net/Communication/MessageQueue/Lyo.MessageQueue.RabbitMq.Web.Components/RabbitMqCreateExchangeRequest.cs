namespace Lyo.MessageQueue.RabbitMq.Web.Components;

/// <summary>Fields gathered by <see cref="RabbitMqCreateExchangeDialog" /> before the workbench declares the exchange.</summary>
public sealed class RabbitMqCreateExchangeRequest
{
    /// <summary>Name of the exchange to declare.</summary>
    public string Name { get; set; } = "";

    /// <summary>Type: direct, topic, fanout, or headers. Defaults to direct.</summary>
    public string Type { get; set; } = "direct";

    /// <summary>Whether the exchange lives through a broker restart. Defaults to true.</summary>
    public bool Durable { get; set; } = true;

    /// <summary>Whether an unused exchange is deleted.</summary>
    public bool AutoDelete { get; set; }
}
