using Lyo.Api.Models.Enums;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Api.Models.Common.Request;

/// <summary>Request that exports query results as CSV, XLSX, or JSON.</summary>
public class ExportRequest
{
    /// <summary>Query to run (filters, sort, projection, and so on). Amount is capped by MaxExportSize.</summary>
    public ProjectionQueryReq Query { get; set; } = null!;

    /// <summary>Export format: Csv, Xlsx, or Json.</summary>
    public ExportFormat Format { get; set; }

    /// <summary>
    /// Optional. Column header to property name or SmartFormat template for Csv/Xlsx. When null or empty, every property is exported using property names as headers. When
    /// IFormatterService is registered, values with '{' are SmartFormat templates (for example "{FirstName} {LastName}",
    /// "{CreatedAt:yyyy-MM-dd}"); otherwise they are property names.
    /// </summary>
    /// <example>{"Email Address": "Email", "Full Name": "{FirstName} {LastName}", "Created": "{CreatedAt:yyyy-MM-dd}"}</example>
    public Dictionary<string, string>? Columns { get; set; }

    /// <summary>
    /// Preferred when set: ordered columns (header to property or template). Use this from UI export so every selected column is kept. <see cref="Columns" /> alone can lose
    /// order and duplicate display headers under case-insensitive handling.
    /// </summary>
    public List<ExportColumnMapping>? ColumnList { get; set; }
}