namespace Lyo.Job.Models.Enums;

/// <summary>Lifecycle state of one registered worker instance.</summary>
public enum JobWorkerInstanceState
{
    Unknown = 0,

    /// <summary>The worker is up and consuming messages.</summary>
    Running = 1,

    /// <summary>The worker is shutting down and finishing runs already in flight.</summary>
    Draining = 2,

    /// <summary>The worker stopped cleanly.</summary>
    Stopped = 3
}