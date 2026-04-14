namespace Lyo.Webhook;

/// <summary>Metric names for <c>Lyo.Metrics.IMetrics</c> (same style as Lyo.Resilience and other Lyo libraries).</summary>
public static class WebhookMetrics
{
    /// <summary>Histogram/timing: end-to-end webhook request (read body, verify, handler).</summary>
    public const string RequestDuration = "lyo.webhook.request.duration";

    /// <summary>Histogram/timing: signature verification only.</summary>
    public const string VerificationDuration = "lyo.webhook.verification.duration";

    /// <summary>Histogram/timing: user handler after successful verification.</summary>
    public const string HandlerDuration = "lyo.webhook.handler.duration";

    /// <summary>Counter: verification failed (signature invalid or missing header).</summary>
    public const string VerificationFailed = "lyo.webhook.verification.failed";

    /// <summary>Counter: verification succeeded.</summary>
    public const string VerificationSucceeded = "lyo.webhook.verification.succeeded";

    /// <summary>Counter: JSON body could not be deserialized (HandleJson).</summary>
    public const string JsonDeserializeFailed = "lyo.webhook.json.deserialize.failed";

    /// <summary>Error recording: unhandled exception from user handler.</summary>
    public const string HandlerError = "lyo.webhook.handler";

    /// <summary>Tag key for route pattern.</summary>
    public const string RouteTag = "route";
}