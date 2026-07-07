namespace Lyo.Job.Alerts;

/// <summary>Configuration for <see cref="JobAlertConsumer" />.</summary>
public sealed class JobAlertsOptions
{
    /// <summary>Default configuration section name.</summary>
    public const string SectionName = "JobAlerts";

    /// <summary>When set, alert payloads are POSTed to this URL as JSON in addition to (or instead of) in-process notification handlers.</summary>
    public string? AlertWebhookUrl { get; set; }
}
