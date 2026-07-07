namespace Lyo.Job.Models.Enums;

/// <summary>Lifecycle state of a registered worker instance.</summary>
public enum JobWorkerInstanceState
{
    Unknown = 0,

    /// <summary>The worker is running and consuming messages.</summary>
    Running = 1,

    /// <summary>The worker is shutting down and finishing in-flight runs.</summary>
    Draining = 2,

    /// <summary>The worker stopped gracefully.</summary>
    Stopped = 3
}
