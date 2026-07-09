namespace Lyo.Job.Client;

/// <summary>Typed HTTP client for the Lyo Job API.</summary>
public interface IJobClient
{
    /// <summary>Run lifecycle, logs, and progress endpoints.</summary>
    JobRunClient Runs { get; }

    /// <summary>Worker instance registration and heartbeat endpoints.</summary>
    JobWorkerInstanceClient WorkerInstances { get; }
}
