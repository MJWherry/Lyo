using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>Defines a schema for CSV validation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvSchema
{
    /// <summary>Gets or sets the list of column definitions.</summary>
    public List<CsvColumn> Columns { get; set; } = new();

    /// <summary>Gets or sets a value indicating whether all columns defined in the schema must be present in the CSV.</summary>
    public bool RequireAllColumns { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether to allow extra columns not defined in the schema.</summary>
    public bool AllowExtraColumns { get; set; } = true;

    public override string ToString() => $"CsvSchema: ColumnsCount={Columns.Count}, RequireAllColumns={RequireAllColumns}, AllowExtraColumns={AllowExtraColumns}";
}