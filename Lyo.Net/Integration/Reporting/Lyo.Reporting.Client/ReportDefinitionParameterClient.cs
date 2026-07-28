using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Models.Response;
using ReportingRoutes = Lyo.Reporting.Models.Constants.Rest.Reporting;

namespace Lyo.Reporting.Client;

/// <summary>Report definition parameter CRUD/query endpoints.</summary>
public sealed class ReportDefinitionParameterClient(IApiClient client, string? routePrefix = null)
{
    public Task<ReportDefinitionParameterRes?> GetAsync(Guid id, IEnumerable<string>? includes = null, CancellationToken ct = default)
        => client.GetAsAsync<ReportDefinitionParameterRes>(
            ReportingRouteBuilder.WithIncludes(ReportingRouteBuilder.Build(routePrefix, $"{ReportingRoutes.DefinitionParameters}/{id}"), includes), ct: ct);

    public Task<CreateResult<ReportDefinitionParameterRes>> CreateAsync(ReportDefinitionParameterReq request, CancellationToken ct = default)
        => client.PostAsAsync<ReportDefinitionParameterReq, CreateResult<ReportDefinitionParameterRes>>(
            ReportingRouteBuilder.Build(routePrefix, ReportingRoutes.DefinitionParameters), request, ct: ct);

    public Task<QueryRes<ReportDefinitionParameterRes>> QueryAsync(QueryConcreteReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryConcreteReq, QueryRes<ReportDefinitionParameterRes>>(
            ReportingRouteBuilder.Build(routePrefix, ReportingRoutes.DefinitionParametersQuery), request, ct: ct);

    public Task<object> UpdateAsync(Guid id, ReportDefinitionParameterReq request, CancellationToken ct = default)
        => client.PutAsAsync<ReportDefinitionParameterReq, object>(ReportingRouteBuilder.Build(routePrefix, $"{ReportingRoutes.DefinitionParameters}/{id}"), request, ct: ct);

    public Task<object> PatchAsync(Guid id, PatchRequest patch, CancellationToken ct = default)
        => client.PatchAsAsync<PatchRequest, object>(ReportingRouteBuilder.Build(routePrefix, $"{ReportingRoutes.DefinitionParameters}/{id}"), patch, ct: ct);

    public Task<object> DeleteAsync(Guid id, CancellationToken ct = default)
        => client.DeleteAsAsync<object>(ReportingRouteBuilder.Build(routePrefix, $"{ReportingRoutes.DefinitionParameters}/{id}"), ct: ct);
}