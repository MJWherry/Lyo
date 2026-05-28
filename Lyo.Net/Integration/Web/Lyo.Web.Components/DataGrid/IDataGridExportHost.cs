using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Web.Components.Models;

namespace Lyo.Web.Components.DataGrid;

/// <summary>Export surface exposed by <see cref="LyoDataGrid{T}" /> and <see cref="LyoDataGridProjected" /> for composable export menu items.</summary>
public interface IDataGridExportHost
{
    bool CanExport { get; }

    bool IsLoading { get; }

    IApiClient ApiClient { get; }

    string ExportRoute { get; }

    CancellationToken CancellationToken { get; }

    /// <summary>When set, column selector derives fields from type reflection (typed grids).</summary>
    Type? ExportDataType { get; }

    /// <summary>When set, column selector uses these field paths (projected grids).</summary>
    IEnumerable<string>? ExportAvailableFields { get; }

    IReadOnlyList<FilterPropertyDefinition>? ExportDisplayNameOverrides { get; }

    IReadOnlyCollection<string>? ExportFieldsUncheckedByDefault { get; }

    bool ExportAllowCustomColumns { get; }

    Task ExportViaApiAsync(ExportFormat format, List<ExportColumnMapping>? columnList, CancellationToken cancellationToken = default);
}
