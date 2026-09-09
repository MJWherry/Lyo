using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Models.Response;
using ReportingRoutes = Lyo.Reporting.Models.Constants.Rest.Reporting;

namespace Lyo.Reporting.Client;

/// <summary>Query and generate calls for report generations.</summary>
public sealed class ReportGenerationClient(IApiClient client, string? routePrefix = null)
{
    public Task<ReportGenerationRes?> GetAsync(Guid id, IEnumerable<string>? includes = null, CancellationToken ct = default)
        => client.GetAsAsync<ReportGenerationRes>(
            ApiRouteBuilder.WithIncludes(ApiRouteBuilder.Build(routePrefix, $"{ReportingRoutes.Generations}/{id}"), includes), ct: ct);

    public Task<QueryRes<ReportGenerationRes>> QueryAsync(QueryConcreteReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryConcreteReq, QueryRes<ReportGenerationRes>>(ApiRouteBuilder.Build(routePrefix, ReportingRoutes.GenerationsQuery), request, ct: ct);

    public Task<ReportGenerationRes> GenerateAsync(GenerateReportReq request, CancellationToken ct = default)
        => client.PostAsAsync<GenerateReportReq, ReportGenerationRes>(ApiRouteBuilder.Build(routePrefix, ReportingRoutes.GenerationsGenerate), request, ct: ct);

    /// <summary>Replays a past generation from its stored snapshot and creates a new generation.</summary>
    /// <param name="id">Generation to replay.</param>
    /// <param name="includeReportData">Echo the composition JSON on the response. Off by default; load it later with <see cref="GetAsync" /> if needed.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<ReportGenerationRes> RerunAsync(Guid id, bool includeReportData = false, CancellationToken ct = default)
    {
        var route = ApiRouteBuilder.Build(routePrefix, $"{ReportingRoutes.Generations}/{id}/{ReportingRoutes.GenerationsRerunSuffix}");
        return client.PostAsAsync<object, ReportGenerationRes>(includeReportData ? $"{route}?includeReportData=true" : route, ct: ct);
    }

    /// <summary>Streams a generation's stored output. The host must configure a download stream factory.</summary>
    public Task<(Stream Content, string? FileName, long? ContentLength)> DownloadAsync(Guid id, CancellationToken ct = default)
        => client.GetFileStreamAsync(ApiRouteBuilder.Build(routePrefix, $"{ReportingRoutes.Generations}/{id}/{ReportingRoutes.GenerationsDownloadSuffix}"), ct: ct);

    /// <summary>Deletes a generation row. The host <c>OnCleanupAsync</c> hook runs first so stored output is removed from storage.</summary>
    public Task<object> DeleteAsync(Guid id, CancellationToken ct = default)
        => client.DeleteAsAsync<object>(ApiRouteBuilder.Build(routePrefix, $"{ReportingRoutes.Generations}/{id}"), ct: ct);
}