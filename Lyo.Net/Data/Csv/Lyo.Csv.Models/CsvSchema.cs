using System.Diagnostics;

namespace Lyo.Csv.Models;

/// <summary>Schema used for CSV validation.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvSchema
{
    /// <summary>Column definitions.</summary>
    public List<CsvColumn> Columns { get; set; } = new();

    /// <summary>If true, every column defined in the schema must appear in the CSV.</summary>
    public bool RequireAllColumns { get; set; } = true;

    /// <summary>If true, extra columns not listed in the schema are allowed.</summary>
    public bool AllowExtraColumns { get; set; } = true;

    /// <inheritdoc />
    public override string ToString() => $"CsvSchema: ColumnsCount={Columns.Count}, RequireAllColumns={RequireAllColumns}, AllowExtraColumns={AllowExtraColumns}";
}