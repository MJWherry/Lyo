using Microsoft.AspNetCore.Components;

namespace Lyo.MessageQueue.RabbitMq.Web.Components;

public partial class RabbitMqPublishEditor
{
    /// <summary>UTF-8 (or JSON) body sent to the selected queue or exchange.</summary>
    [Parameter]
    public string MessageBody { get; set; } = "";

    /// <summary>Fired when the body editor changes.</summary>
    [Parameter]
    public EventCallback<string> MessageBodyChanged { get; set; }

    /// <summary>If true, the body is wrapped in <c>QueueMessageEnvelope</c> before publish.</summary>
    [Parameter]
    public bool WrapInEnvelope { get; set; }

    /// <summary>Fired when envelope wrapping is toggled.</summary>
    [Parameter]
    public EventCallback<bool> WrapInEnvelopeChanged { get; set; }

    /// <summary>Optional envelope message id.</summary>
    [Parameter]
    public string EnvelopeMessageId { get; set; } = "";

    /// <summary>Fired when the envelope message id changes.</summary>
    [Parameter]
    public EventCallback<string> EnvelopeMessageIdChanged { get; set; }

    /// <summary>Optional envelope trace id.</summary>
    [Parameter]
    public string EnvelopeTraceId { get; set; } = "";

    /// <summary>Fired when the envelope trace id changes.</summary>
    [Parameter]
    public EventCallback<string> EnvelopeTraceIdChanged { get; set; }

    /// <summary>AMQP priority 0–255. Zero uses the broker default.</summary>
    [Parameter]
    public int MessagePriority { get; set; }

    /// <summary>Fired when priority changes.</summary>
    [Parameter]
    public EventCallback<int> MessagePriorityChanged { get; set; }

    /// <summary>If true, delay and Send delayed are shown (queue publish).</summary>
    [Parameter]
    public bool ShowDelay { get; set; }

    /// <summary>Delay, in seconds, for a delayed queue publish.</summary>
    [Parameter]
    public double SendDelaySeconds { get; set; }

    /// <summary>Fired when delay changes.</summary>
    [Parameter]
    public EventCallback<double> SendDelaySecondsChanged { get; set; }

    /// <summary>If true, the routing-key field sits on the send row (exchange publish).</summary>
    [Parameter]
    public bool ShowRoutingKey { get; set; }

    /// <summary>Routing key used when publishing to an exchange.</summary>
    [Parameter]
    public string RoutingKey { get; set; } = "";

    /// <summary>Fired when the routing key changes.</summary>
    [Parameter]
    public EventCallback<string> RoutingKeyChanged { get; set; }

    /// <summary>Caption of the primary send button.</summary>
    [Parameter]
    public string SendLabel { get; set; } = "Send";

    /// <summary>Disables send buttons while a call is running.</summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>Publish now.</summary>
    [Parameter]
    public EventCallback OnSend { get; set; }

    /// <summary>Delayed publish (queues only).</summary>
    [Parameter]
    public EventCallback OnSendDelayed { get; set; }
}
