using Lyo.Api.Client;
using Lyo.Exceptions;

namespace Lyo.Job.Client;

/// <summary>Typed sub-client for the Lyo Job API. Compose on any <see cref="IApiClient" /> implementation.</summary>
public sealed class JobClient : IJobClient
{
    public JobClient(IApiClient apiClient, JobClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(apiClient);
        var routePrefix = options?.RoutePrefix;
        Definitions = new(apiClient, routePrefix);
        Runs = new(apiClient, routePrefix);
        WorkerInstances = new(apiClient, routePrefix);
    }

    public JobDefinitionClient Definitions { get; }

    public JobRunClient Runs { get; }

    public JobWorkerInstanceClient WorkerInstances { get; }
}