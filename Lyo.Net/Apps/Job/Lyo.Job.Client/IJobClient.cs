namespace Lyo.Job.Client;

/// <summary>Typed HTTP client for Lyo Job API.</summary>
public interface IJobClient
{
    /// <summary>Job definition query calls.</summary>
    JobDefinitionClient Definitions { get; }

    /// <summary>Run lifecycle, log, and progress calls.</summary>
    JobRunClient Runs { get; }

    /// <summary>Worker instance register and heartbeat calls.</summary>
    JobWorkerInstanceClient WorkerInstances { get; }
}