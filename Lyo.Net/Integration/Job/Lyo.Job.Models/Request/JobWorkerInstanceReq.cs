using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Request;

/// <summary>Registration/heartbeat payload for a running worker instance.</summary>
public sealed class JobWorkerInstanceReq
{
    /// <summary>Worker type this instance consumes.</summary>
    public string WorkerType { get; set; } = null!;

    /// <summary>Machine (host) name the worker process is running on.</summary>
    public string MachineName { get; set; } = null!;

    /// <summary>OS process id of the worker host.</summary>
    public int ProcessId { get; set; }

    /// <summary>Lifecycle state of the instance.</summary>
    public JobWorkerInstanceState State { get; set; } = JobWorkerInstanceState.Running;

    /// <summary>Number of runs currently being executed by this instance.</summary>
    public int InFlightCount { get; set; }

    /// <summary>UTC timestamp when the worker registered.</summary>
    public DateTime StartedTimestamp { get; set; }

    /// <summary>UTC timestamp of the heartbeat.</summary>
    public DateTime LastHeartbeatUtc { get; set; }
}
