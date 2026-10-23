using System.Diagnostics;
using Lyo.Exceptions.Models;

namespace Lyo.Job.Workers.Notification;

/// <summary>Options for <see cref="SmsNotificationWorker" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SmsNotificationWorkerOptions
{
    /// <summary>Default name of the configuration section.</summary>
    public const string SectionName = "SmsNotificationWorker";

    /// <summary>Worker type this instance subscribes to.</summary>
    public string WorkerType { get; set; } = Constants.WorkerType.Sms;

    /// <summary>Sender used when the run does not supply a <c>From</c> parameter. Null falls back to the SMS service's configured default.</summary>
    public string? DefaultFrom { get; set; }

    /// <summary>Max requeue attempts for the input queue message before it goes to the DLQ. Null uses the worker default.</summary>
    public int? MaxRequeueCount { get; set; }

    /// <summary>Dead-letter queue name. Null drops messages that exceed the requeue limit.</summary>
    public string? DlqName { get; set; }

    /// <summary>Checks the options and returns the list of validation failures (empty when valid).</summary>
    public IReadOnlyList<string> GetValidationErrors() => string.IsNullOrWhiteSpace(WorkerType) ? [$"{nameof(WorkerType)} is required."] : [];

    /// <summary>Throws when the options are invalid.</summary>
    /// <exception cref="ValidationException">One or more options are invalid.</exception>
    public void Validate()
    {
        var errors = GetValidationErrors();
        if (errors.Count > 0)
            throw new ValidationException($"Invalid {nameof(SmsNotificationWorkerOptions)}: {string.Join(" ", errors)}");
    }

    /// <summary>Worker type, default sender, and requeue limit.</summary>
    public override string ToString() => $"WorkerType={WorkerType} From={DefaultFrom} MaxRequeue={MaxRequeueCount} Dlq={DlqName}";
}