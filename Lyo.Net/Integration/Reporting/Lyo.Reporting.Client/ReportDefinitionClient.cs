using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Models.Response;
using ReportingRoutes = Lyo.Reporting.Models.Constants.Rest.Reporting;

namespace Lyo.Reporting.Client;

/// <summary>Report definition CRUD/query endpoints.</summary>
public sealed class ReportDefinitionClient(IApiClient client, string? routePrefix = null)
{
    public Task<ReportDefinitionRes?> GetAsync(Guid id, IEnumerable<string>? includes = null, CancellationToken ct = default)
        => client.GetAsAsync<ReportDefinitionRes>(
            ReportingRouteBuilder.WithIncludes(ReportingRouteBuilder.Build(routePrefix, $"{ReportingRoutes.Definitions}/{id}"), includes), ct: ct);

    public Task<CreateResult<ReportDefinitionRes>> CreateAsync(ReportDefinitionReq request, CancellationToken ct = default)
        => client.PostAsAsync<ReportDefinitionReq, CreateResult<ReportDefinitionRes>>(ReportingRouteBuilder.Build(routePrefix, ReportingRoutes.Definitions), request, ct: ct);

    public Task<QueryRes<ReportDefinitionRes>> QueryAsync(QueryConcreteReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryConcreteReq, QueryRes<ReportDefinitionRes>>(ReportingRouteBuilder.Build(routePrefix, ReportingRoutes.DefinitionsQuery), request, ct: ct);

    public Task<object> UpdateAsync(Guid id, ReportDefinitionReq request, CancellationToken ct = default)
        => client.PutAsAsync<ReportDefinitionReq, object>(ReportingRouteBuilder.Build(routePrefix, $"{ReportingRoutes.Definitions}/{id}"), request, ct: ct);

    public Task<object> PatchAsync(Guid id, PatchRequest patch, CancellationToken ct = default)
        => client.PatchAsAsync<PatchRequest, object>(ReportingRouteBuilder.Build(routePrefix, $"{ReportingRoutes.Definitions}/{id}"), patch, ct: ct);

    public Task<object> DeleteAsync(Guid id, CancellationToken ct = default)
        => client.DeleteAsAsync<object>(ReportingRouteBuilder.Build(routePrefix, $"{ReportingRoutes.Definitions}/{id}"), ct: ct);
}