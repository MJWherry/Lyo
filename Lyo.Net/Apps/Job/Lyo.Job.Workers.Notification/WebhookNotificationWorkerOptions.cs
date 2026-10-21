using Lyo.Exceptions.Models;

namespace Lyo.Job.Workers.Notification;

/// <summary>Options for <see cref="WebhookNotificationWorker" />.</summary>
public sealed class WebhookNotificationWorkerOptions
{
    /// <summary>Default name of the configuration section.</summary>
    public const string SectionName = "WebhookNotificationWorker";

    /// <summary>Worker type this instance subscribes to. Must match <c>WorkerType</c> on the job definitions its workflow steps point at.</summary>
    public string WorkerType { get; set; } = Constants.WorkerType.Webhook;

    /// <summary>
    /// Extra attempts after the first for a retryable response (408, 429, 5xx) or a transport error. The run itself is also retried by the job scheduler, so keep this small — it
    /// exists to ride out a blip, not to replace job-level retry. Default 2.
    /// </summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>Delay before the first retry; doubled each attempt. Default 1 second.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Per-attempt request timeout. Default 30 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Max requeue attempts for the input queue message before it goes to the DLQ. Null uses the worker default.</summary>
    public int? MaxRequeueCount { get; set; }

    /// <summary>Dead-letter queue name. Null drops messages that exceed the requeue limit.</summary>
    public string? DlqName { get; set; }

    /// <summary>Checks the options and returns the list of validation failures (empty when valid).</summary>
    public IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(WorkerType))
            errors.Add($"{nameof(WorkerType)} is required.");

        if (RetryCount < 0)
            errors.Add($"{nameof(RetryCount)} must be 0 or greater.");

        if (RetryDelay < TimeSpan.Zero)
            errors.Add($"{nameof(RetryDelay)} must be 0 or greater.");

        if (Timeout <= TimeSpan.Zero)
            errors.Add($"{nameof(Timeout)} must be greater than 0.");

        return errors;
    }

    /// <summary>Throws when the options are invalid.</summary>
    /// <exception cref="ValidationException">One or more options are invalid.</exception>
    public void Validate()
    {
        var errors = GetValidationErrors();
        if (errors.Count > 0)
            throw new ValidationException($"Invalid {nameof(WebhookNotificationWorkerOptions)}: {string.Join(" ", errors)}");
    }
}