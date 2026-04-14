using Lyo.Api.Models.Enums;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Api.Models.Common.Request;

/// <summary>Request to export query results as CSV, XLSX, or JSON.</summary>
public class ExportRequest
{
    /// <summary>The query to execute (filters, sort, projection, etc.). Amount is capped by MaxExportSize.</summary>
    public ProjectionQueryReq Query { get; set; } = null!;

    /// <summary>Export format: Csv, Xlsx, or Json.</summary>
    public ExportFormat Format { get; set; }

    /// <summary>
    /// Optional. Column header -> property name or SmartFormat template for Csv/Xlsx. When null or empty, all properties are exported with property names as headers. When
    /// IFormatterService is registered: values with '{' are SmartFormat templates (e.g. "{FirstName} {LastName}", "{CreatedAt:yyyy-MM-dd}"); otherwise treated as property name.
    /// </summary>
    /// <example>{"Email Address": "Email", "Full Name": "{FirstName} {LastName}", "Created": "{CreatedAt:yyyy-MM-dd}"}</example>
    public Dictionary<string, string>? Columns { get; set; }

    /// <summary>
    /// Preferred when set: ordered columns (header → property or template). Use this from UI export so every selected column is preserved; <see cref="Columns" /> alone can lose
    /// order and duplicate display headers under case-insensitive handling.
    /// </summary>
    public List<ExportColumnMapping>? ColumnList { get; set; }
}