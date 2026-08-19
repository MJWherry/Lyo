using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Models.Response;
using ReportingRoutes = Lyo.Reporting.Models.Constants.Rest.Reporting;

namespace Lyo.Reporting.Client;

/// <summary>Report generation query and generate endpoints.</summary>
public sealed class ReportGenerationClient(IApiClient client, string? routePrefix = null)
{
    public Task<ReportGenerationRes?> GetAsync(Guid id, IEnumerable<string>? includes = null, CancellationToken ct = default)
        => client.GetAsAsync<ReportGenerationRes>(
            ReportingRouteBuilder.WithIncludes(ReportingRouteBuilder.Build(routePrefix, $"{ReportingRoutes.Generations}/{id}"), includes), ct: ct);

    public Task<QueryRes<ReportGenerationRes>> QueryAsync(QueryConcreteReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryConcreteReq, QueryRes<ReportGenerationRes>>(ReportingRouteBuilder.Build(routePrefix, ReportingRoutes.GenerationsQuery), request, ct: ct);

    public Task<ReportGenerationRes> GenerateAsync(GenerateReportReq request, CancellationToken ct = default)
        => client.PostAsAsync<GenerateReportReq, ReportGenerationRes>(ReportingRouteBuilder.Build(routePrefix, ReportingRoutes.GenerationsGenerate), request, ct: ct);

    /// <summary>Re-runs a past generation from its stored snapshot, producing a new generation.</summary>
    public Task<ReportGenerationRes> RerunAsync(Guid id, CancellationToken ct = default)
        => client.PostAsAsync<object, ReportGenerationRes>(
            ReportingRouteBuilder.Build(routePrefix, $"{ReportingRoutes.Generations}/{id}/{ReportingRoutes.GenerationsRerunSuffix}"), ct: ct);

    /// <summary>Streams a generation's persisted output. Requires the host to configure a download stream factory.</summary>
    public Task<(Stream Content, string? FileName, long? ContentLength)> DownloadAsync(Guid id, CancellationToken ct = default)
        => client.GetFileStreamAsync(ReportingRouteBuilder.Build(routePrefix, $"{ReportingRoutes.Generations}/{id}/{ReportingRoutes.GenerationsDownloadSuffix}"), ct: ct);

    /// <summary>Deletes a generation row. The host <c>OnCleanupAsync</c> hook runs first so persisted output is removed from storage.</summary>
    public Task<object> DeleteAsync(Guid id, CancellationToken ct = default)
        => client.DeleteAsAsync<object>(ReportingRouteBuilder.Build(routePrefix, $"{ReportingRoutes.Generations}/{id}"), ct: ct);
}