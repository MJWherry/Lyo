using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Job.Models.Response;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using JobRoutes = Lyo.Job.Models.Constants.Rest.Job;

namespace Lyo.Job.Client;

/// <summary>Job definition query endpoints.</summary>
public sealed class JobDefinitionClient(IApiClient client, string? routePrefix = null)
{
    public Task<QueryRes<JobDefinitionRes>> QueryAsync(QueryConcreteReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryConcreteReq, QueryRes<JobDefinitionRes>>(JobRouteBuilder.Build(routePrefix, JobRoutes.DefinitionsQuery), request, ct: ct);

    /// <summary>Returns distinct non-empty <see cref="JobDefinitionRes.WorkerType" /> values from the Job API.</summary>
    public async Task<IReadOnlyList<string>> GetDistinctWorkerTypesAsync(CancellationToken ct = default)
    {
        var query = new QueryConcreteReqBuilder().Build();
        var results = await QueryAsync(query, ct).ConfigureAwait(false);
        if (results.Items is null || !results.IsSuccess)
            return [];

        return results.Items.Select(d => d.WorkerType).Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}