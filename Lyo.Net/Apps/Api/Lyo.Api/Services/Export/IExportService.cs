using System.Linq.Expressions;
using Lyo.Api.Models.Common.Request;
using Lyo.Common.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Api.Services.Export;

/// <summary>Exports query results to CSV, XLSX, or JSON.</summary>
// ReSharper disable once UnusedTypeParameter
public interface IExportService<TContext>
    where TContext : DbContext
{
    /// <summary>Exports data matching the request query to the given format.</summary>
    /// <param name="request">Export request with query, format, and optional column mapping.</param>
    /// <param name="defaultOrder">Default sort expression when the query has no sort.</param>
    /// <param name="defaultSortDirection">Default sort direction.</param>
    /// <param name="ct">Token used to cancel the export.</param>
    /// <returns>Tuple of (stream, contentType, fileName). The caller must dispose the stream.</returns>
    Task<(Stream Stream, string ContentType, string FileName)> ExportAsync<TDbEntity, TResponse>(
        ExportRequest request,
        Expression<Func<TDbEntity, object?>> defaultOrder,
        SortDirection defaultSortDirection = SortDirection.Desc,
        CancellationToken ct = default)
        where TDbEntity : class;
}