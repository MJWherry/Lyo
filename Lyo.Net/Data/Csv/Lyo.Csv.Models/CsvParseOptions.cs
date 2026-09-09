using System.Diagnostics;
using Lyo.DataTable.Models;

namespace Lyo.Csv.Models;

/// <summary>Options that control CSV parse behavior.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CsvParseOptions
{
    /// <summary>If true, keep parsing when errors occur. Starts as false.</summary>
    public bool ContinueOnError { get; set; }

    /// <summary>Optional callback invoked when a parse error occurs.</summary>
    public Action<CsvParseError>? OnError { get; set; }

    /// <summary>Optional filter that skips certain rows during parse.</summary>
    public Func<Dictionary<string, string>, bool>? RowFilter { get; set; }

    /// <summary>Max rows to parse. If null, all rows are parsed.</summary>
    public int? MaxRows { get; set; }

    /// <summary>
    /// Pooling options for CSV → DataTable imports (value interning). Format pooling is unused for CSV. Starts matching <see cref="CsvOptions.CreateDefaultPooling" /> (
    /// <c>PoolValues=false</c>).
    /// </summary>
    public DataTablePoolingOptions Pooling { get; set; } = CsvOptions.CreateDefaultPooling();

    /// <inheritdoc />
    public override string ToString()
        => $"CsvParseOptions: ContinueOnError={ContinueOnError}, HasOnError={OnError != null}, HasRowFilter={RowFilter != null}, MaxRows={MaxRows?.ToString() ?? "All"}, Pooling=({Pooling})";
}