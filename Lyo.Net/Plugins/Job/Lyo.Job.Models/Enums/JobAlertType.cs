namespace Lyo.Job.Models.Enums;

/// <summary>Kinds of job alerts published to <c>job.notifications.alert</c>.</summary>
public enum JobAlertType
{
    Failure,
    CircuitBreakerTripped,
    DeadJob,
    SlaBreach
}