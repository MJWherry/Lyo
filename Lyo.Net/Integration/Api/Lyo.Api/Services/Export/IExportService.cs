using System.Linq.Expressions;
using Lyo.Api.Models.Common.Request;
using Lyo.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Export;

/// <summary>Exports query results to CSV, XLSX, or JSON format.</summary>
public interface IExportService<TContext>
    where TContext : DbContext
{
    /// <summary>Exports data matching the request query to the specified format.</summary>
    /// <param name="request">Export request with query, format, and optional column mapping.</param>
    /// <param name="defaultOrder">Default sort expression when query has no sort.</param>
    /// <param name="defaultSortDirection">Default sort direction.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple of (stream, contentType, fileName). Caller must dispose the stream.</returns>
    Task<(Stream Stream, string ContentType, string FileName)> ExportAsync<TDbEntity, TResponse>(
        ExportRequest request,
        Expression<Func<TDbEntity, object?>> defaultOrder,
        SortDirection defaultSortDirection = SortDirection.Desc,
        CancellationToken ct = default)
        where TDbEntity : class;
}