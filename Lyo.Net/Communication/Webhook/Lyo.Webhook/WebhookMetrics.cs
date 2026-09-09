namespace Lyo.Webhook;

/// <summary>Metric names for <c>Lyo.Metrics.IMetrics</c>, matching the style used by Lyo.Resilience and other Lyo libraries.</summary>
public static class WebhookMetrics
{
    /// <summary>Histogram/timer: end-to-end webhook request (read body, verify, handler).</summary>
    public const string RequestDuration = "lyo.webhook.request.duration";

    /// <summary>Histogram/timer: signature verification only.</summary>
    public const string VerificationDuration = "lyo.webhook.verification.duration";

    /// <summary>Histogram/timer: user handler after a successful verification.</summary>
    public const string HandlerDuration = "lyo.webhook.handler.duration";

    /// <summary>Counter: verification failed (invalid signature or missing header).</summary>
    public const string VerificationFailed = "lyo.webhook.verification.failed";

    /// <summary>Counter: verification succeeded for the request.</summary>
    public const string VerificationSucceeded = "lyo.webhook.verification.succeeded";

    /// <summary>Counter: JSON body could not be deserialized (<c>HandleJson</c>).</summary>
    public const string JsonDeserializeFailed = "lyo.webhook.json.deserialize.failed";

    /// <summary>Error recording: unhandled exception from the user handler.</summary>
    public const string HandlerError = "lyo.webhook.handler";

    /// <summary>Tag key whose value is the route pattern.</summary>
    public const string RouteTag = "route";
}