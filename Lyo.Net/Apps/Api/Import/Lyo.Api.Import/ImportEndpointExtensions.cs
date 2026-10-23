using System.Text.Json;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lyo.Api.Import;

/// <summary>
/// Maps <c>POST /Import</c>. Rows are returned to the caller. The endpoint does not write a store. Hosts that know the row type call <see cref="IImportService.Import{T}" />
/// directly.
/// </summary>
public static class ImportEndpointExtensions
{
    /// <summary>
    /// POST <paramref name="pattern" /> with an <see cref="ImportRequest" /> body. Each good JSON object is a property bag. CSV and XLSX hosts should call
    /// <see cref="IImportService.Import{T}" /> with their row type.
    /// </summary>
    public static IEndpointRouteBuilder MapLyoImport(this IEndpointRouteBuilder endpoints, string pattern = "/Import")
    {
        endpoints.MapPost(
            pattern, (ImportRequest request, IImportService service) => {
                var result = service.Import<Dictionary<string, JsonElement>>(request);
                return Results.Ok(
                    new ImportResponse {
                        IsSuccess = result.IsSuccess,
                        Message = result.Message,
                        RowsJson = JsonService.Default.ExportToJsonString(result.Rows),
                        Issues = result.Issues.ToList()
                    });
            });

        return endpoints;
    }
}