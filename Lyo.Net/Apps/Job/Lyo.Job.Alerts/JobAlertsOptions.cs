namespace Lyo.Job.Alerts;

/// <summary>Settings for <see cref="JobAlertConsumer" />.</summary>
public sealed class JobAlertsOptions
{
    /// <summary>Default configuration section.</summary>
    public const string SectionName = "JobAlerts";

    /// <summary>When set, alert payloads are POSTed here as JSON, in addition to or instead of in-process notification handlers.</summary>
    public string? AlertWebhookUrl { get; set; }

    /// <summary>
    /// Per-request timeout for the webhook POST, applied to the named <c>HttpClient</c> at registration. Without it the handler keeps the 100-second default, so one hung
    /// endpoint stalls alert delivery for every definition. Default 10 seconds.
    /// </summary>
    public TimeSpan WebhookTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Extra webhook tries after the first failure. Covers transient failures (connection resets, 5xx, timeouts) without putting the message back on the queue.
    /// Zero turns retries off. Default 2.
    /// </summary>
    public int WebhookRetryCount { get; set; } = 2;

    /// <summary>Base wait between webhook attempts; doubles each time. Default 1 second.</summary>
    public TimeSpan WebhookRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long to suppress repeat alerts with the same definition and alert type. A definition failing on a tight schedule (or a retry storm) otherwise floods every
    /// notification channel with the same message. <see cref="TimeSpan.Zero" /> turns dedup off and sends every alert. Default 5 minutes.
    /// </summary>
    public TimeSpan DedupWindow { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Max definition/alert-type keys kept for dedup. Going over evicts the oldest entries so the window cannot grow without a cap. Default 5 000.</summary>
    public int DedupCacheLimit { get; set; } = 5_000;

    /// <summary>
    /// Queue that gets the raw bytes of alert messages that cannot be deserialized. Without it a poison message is acked and lost. Null or empty drops poison
    /// messages instead of keeping them. Default <c>job.notifications.alert.dlq</c>.
    /// </summary>
    public string? DeadLetterQueueName { get; set; } = "job.notifications.alert.dlq";

    /// <summary>Validation problems for the current values. Empty when valid.</summary>
    public IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();
        if (WebhookTimeout <= TimeSpan.Zero)
            errors.Add($"{nameof(WebhookTimeout)} must be greater than zero.");

        if (WebhookRetryCount < 0)
            errors.Add($"{nameof(WebhookRetryCount)} must not be negative.");

        if (WebhookRetryDelay < TimeSpan.Zero)
            errors.Add($"{nameof(WebhookRetryDelay)} must not be negative.");

        if (DedupWindow < TimeSpan.Zero)
            errors.Add($"{nameof(DedupWindow)} must not be negative.");

        if (DedupCacheLimit <= 0)
            errors.Add($"{nameof(DedupCacheLimit)} must be greater than zero.");

        if (!string.IsNullOrWhiteSpace(AlertWebhookUrl) && !Uri.TryCreate(AlertWebhookUrl, UriKind.Absolute, out _))
            errors.Add($"{nameof(AlertWebhookUrl)} must be an absolute URL.");

        return errors;
    }

    /// <summary>Throws if any option is invalid. Call from host startup so it fails before the first alert, not during it.</summary>
    /// <exception cref="InvalidOperationException">One or more options are invalid.</exception>
    public void Validate()
    {
        var errors = GetValidationErrors();
        if (errors.Count > 0)
            throw new InvalidOperationException($"Invalid {nameof(JobAlertsOptions)}: {string.Join(" ", errors)}");
    }
}
