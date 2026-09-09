using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Reporting.Models.Response;

namespace Lyo.Reporting.Web.Components;

/// <summary>
/// Fetches a generation's composition JSON when needed. Generate and rerun responses omit it (see <see cref="ReportGenerationRes.ReportDataJson" />), so the tabular preview reads
/// it back only when the operator opens the output.
/// </summary>
public static class ReportGenerationDataLoader
{
    /// <summary>Builds the loader delegate that <c>ReportGenerationView.LoadReportDataAsync</c> expects.</summary>
    /// <param name="apiClient">Client used for the read-back query.</param>
    /// <param name="generationRoute">Generation route, for example <c>Reporting/Generation</c>.</param>
    public static Func<Guid, CancellationToken, Task<string?>> Create(IApiClient apiClient, string generationRoute)
        => (id, ct) => LoadAsync(apiClient, generationRoute, id, ct);

    private static async Task<string?> LoadAsync(IApiClient apiClient, string generationRoute, Guid id, CancellationToken ct)
    {
        var req = new QueryConcreteReq { Keys = [[id]], Amount = 1 };
        var res = await apiClient.PostAsAsync<QueryConcreteReq, QueryRes<ReportGenerationRes>>($"{generationRoute}/QueryConcrete", req, ct: ct).ConfigureAwait(false);
        return res?.Items?.FirstOrDefault()?.ReportDataJson;
    }
}
